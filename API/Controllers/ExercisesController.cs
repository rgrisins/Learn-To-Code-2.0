using System.Security.Claims;
using System.Text.Json;
using LearnToCode.API.Contracts.Exercises;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Route("api/exercises")]
public class ExercisesController : ControllerBase
{
    // Minimālais testu skaits uzdevumam, ieskaitot redzamos un slēptos testus.
    private const int MinimumTotalExerciseTestCases = 20;
    // Pirmie divi testi vienmēr ir redzamie paraugi.
    private const int SampleTestCaseCount = 2;
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly DockerCodeRunnerService _runner;

    public ExercisesController(AppDbContext db, DockerCodeRunnerService runner)
    {
        _db = db;
        _runner = runner;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ExerciseListItemDto>>> GetAll(CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var solvedIds = userId.HasValue
            ? (await _db.ExerciseSubmissions
                .Where(s => s.UserId == userId.Value && s.Status == SubmissionStatus.Passed)
                .Select(s => s.ExerciseId)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet()
            : [];

        var exercises = await _db.Exercises
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Id)
            .Select(e => new
            {
                e.Id, e.Title, e.Description, e.Difficulty,
                e.LanguageCode, e.LanguageVersion,
                TestCaseCount = e.TestCases.Count,
            })
            .ToListAsync(ct);

        // Saraksta skatam aprēķina kopējo iesniegumu un atrisinājumu statistiku.
        var exerciseIds = exercises.Select(e => e.Id).ToList();
        var stats = await _db.ExerciseSubmissions
            .Where(s => exerciseIds.Contains(s.ExerciseId))
            .GroupBy(s => s.ExerciseId)
            .Select(group => new
            {
                ExerciseId = group.Key,
                SubmissionCount = group.Count(),
                AttemptedUserCount = group.Select(s => s.UserId).Distinct().Count(),
                SolvedUserCount = group
                    .Where(s => s.Status == SubmissionStatus.Passed)
                    .Select(s => s.UserId)
                    .Distinct()
                    .Count(),
            })
            .ToDictionaryAsync(item => item.ExerciseId, ct);

        return Ok(exercises.Select(e => new ExerciseListItemDto(
            e.Id, e.Title, e.Description, e.Difficulty,
            e.LanguageCode, e.LanguageVersion,
            e.TestCaseCount,
            stats.TryGetValue(e.Id, out var item) ? item.SubmissionCount : 0,
            stats.TryGetValue(e.Id, out item) ? item.AttemptedUserCount : 0,
            stats.TryGetValue(e.Id, out item) && item.AttemptedUserCount > 0
                ? (int)Math.Round(item.SolvedUserCount * 100.0 / item.AttemptedUserCount)
                : 0,
            solvedIds.Contains(e.Id)
        )));
    }

    [HttpGet("submissions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ExerciseSubmissionDto>>> GetMySubmissions(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var submissions = await _db.ExerciseSubmissions
            .AsNoTracking()
            .Include(s => s.Exercise)
            .Where(s => s.UserId == userId.Value)
            .OrderByDescending(s => s.SubmittedAtUtc)
            .ThenByDescending(s => s.Id)
            .ToListAsync(ct);

        return Ok(submissions.Select(MapSubmission));
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ExerciseDetailDto>> GetById(int id, CancellationToken ct)
    {
        var exercise = await _db.Exercises
            .Include(e => e.TestCases)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (exercise is null)
            return NotFound(new { message = "Uzdevums nav atrasts." });

        var userId = GetCurrentUserId();
        var isSolved = userId.HasValue && await _db.ExerciseSubmissions
            .AnyAsync(s => s.ExerciseId == id && s.UserId == userId.Value
                           && s.Status == SubmissionStatus.Passed, ct);
        var hasPendingDescriptionEditRequest = userId.HasValue && await _db.ExerciseRequests
            .AnyAsync(r =>
                r.ExerciseId == id &&
                r.AuthorId == userId.Value &&
                r.RequestType == ExerciseRequestType.EditDescription &&
                r.Status == ExerciseRequestStatus.Pending,
                ct);

        var visible = exercise.TestCases
            .Where(tc => !tc.IsHidden)
            .OrderBy(tc => tc.OrderIndex)
            .Select(tc => new ExerciseTestCaseDto(tc.Id, tc.Input, tc.ExpectedOutput, tc.OrderIndex));

        return Ok(new ExerciseDetailDto(
            exercise.Id, exercise.Title, exercise.Description, exercise.Difficulty,
            exercise.LanguageCode, exercise.LanguageVersion,
            visible, isSolved, hasPendingDescriptionEditRequest
        ));
    }

    [HttpPost("{id:int}/description-request")]
    [Authorize(Roles = $"{nameof(UserRole.Pedagogs)},{nameof(UserRole.Administrators)}")]
    public async Task<IActionResult> SubmitDescriptionEditRequest(
        int id, [FromBody] UpdateExerciseDescriptionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var exercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null)
            return NotFound(new { message = "Uzdevums nav atrasts." });

        var description = NormalizeMultilineText(request.Description);
        if (string.IsNullOrWhiteSpace(description))
            return BadRequest(new { message = "Apraksts nedrīkst būt tukšs." });

        if (string.Equals(description, exercise.Description.Trim(), StringComparison.Ordinal))
            return BadRequest(new { message = "Apraksts nav mainīts." });

        var hasPending = await _db.ExerciseRequests.AnyAsync(r =>
            r.ExerciseId == id &&
            r.AuthorId == userId.Value &&
            r.RequestType == ExerciseRequestType.EditDescription &&
            r.Status == ExerciseRequestStatus.Pending,
            ct);

        if (hasPending)
            return Conflict(new { message = "Šim uzdevumam jau ir tavs gaidošs apraksta labojuma pieprasījums." });

        // Apraksta labojums nonāk apstiprināšanai, nevis uzreiz maina uzdevumu.
        _db.ExerciseRequests.Add(new ExerciseRequest
        {
            RequestType = ExerciseRequestType.EditDescription,
            ExerciseId = exercise.Id,
            AuthorId = userId.Value,
            Title = exercise.Title,
            Description = description,
            Difficulty = exercise.Difficulty,
            LanguageCode = exercise.LanguageCode,
            LanguageVersion = exercise.LanguageVersion,
            SolutionLanguageCode = exercise.LanguageCode,
            SolutionCode = string.Empty,
            TestCasesJson = "[]",
            Status = ExerciseRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return Accepted(new { message = "Apraksta labojuma pieprasījums nosūtīts administratoram." });
    }

    [HttpPost("{id:int}/submit")]
    [Authorize]
    public async Task<ActionResult<SubmissionResultDto>> Submit(
        int id, [FromBody] SubmitCodeRequest request, CancellationToken ct)
    {
        var exercise = await FindExerciseWithTestsAsync(id, ct);
        if (exercise is null)
            return NotFound(new { message = "Uzdevums nav atrasts." });

        var validationError = ValidateSubmissionRequest(request, out var language);
        if (validationError is not null)
            return BadRequest(new { message = validationError });

        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        // Ja uzdevums jau atrisināts, atkārtota iesniegšana nav atļauta.
        var alreadySolved = await _db.ExerciseSubmissions
            .AnyAsync(s => s.ExerciseId == exercise.Id && s.UserId == userId.Value
                           && s.Status == SubmissionStatus.Passed, ct);

        if (alreadySolved)
        {
            return Conflict(new { message = "Šis uzdevums jau ir atrisināts. Atkārtota iesniegšana nav atļauta." });
        }

        var result = await ExecuteSubmissionAsync(exercise, userId.Value, request.Code, language, null, ct);

        return Ok(result);
    }

    [HttpPost("{id:int}/submit-stream")]
    [Authorize]
    public async Task SubmitStream(int id, [FromBody] SubmitCodeRequest request, CancellationToken ct)
    {
        var exercise = await FindExerciseWithTestsAsync(id, ct);
        if (exercise is null)
        {
            await WriteJsonErrorAsync(StatusCodes.Status404NotFound, "Uzdevums nav atrasts.", ct);
            return;
        }

        var validationError = ValidateSubmissionRequest(request, out var language);
        if (validationError is not null)
        {
            await WriteJsonErrorAsync(StatusCodes.Status400BadRequest, validationError, ct);
            return;
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            await WriteJsonErrorAsync(StatusCodes.Status401Unauthorized, "Lietotājs nav autorizēts.", ct);
            return;
        }

        var alreadySolved = await _db.ExerciseSubmissions
            .AnyAsync(s => s.ExerciseId == exercise.Id && s.UserId == userId.Value
                           && s.Status == SubmissionStatus.Passed, ct);

        if (alreadySolved)
        {
            await WriteJsonErrorAsync(StatusCodes.Status409Conflict,
                "Šis uzdevums jau ir atrisināts. Atkārtota iesniegšana nav atļauta.", ct);
            return;
        }

        Response.ContentType = "application/x-ndjson; charset=utf-8";

        var result = await ExecuteSubmissionAsync(
            exercise,
            userId.Value,
            request.Code,
            language,
            async (progress, token) => await WriteJsonLineAsync(Response, progress, token),
            ct);

        await WriteJsonLineAsync(Response, new SubmissionProgressDto(
            "completed",
            result.Id,
            result.TestResults.Count(),
            result.TestsTotal,
            result.TestsPassed,
            result.Status,
            result.ErrorMessage,
            null,
            result
        ), ct);
    }

    [HttpPost]
    [Authorize(Roles = $"{nameof(UserRole.Pedagogs)},{nameof(UserRole.Administrators)}")]
    public async Task<IActionResult> SubmitForApproval(
        [FromBody] CreateExerciseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Nosaukums nedrīkst būt tukšs." });

        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Apraksts nedrīkst būt tukšs." });

        var difficulty = NormalizeDifficulty(request.Difficulty);
        if (difficulty is null)
            return BadRequest(new { message = "Grūtībai jābūt: viegls, vidējs vai grūts." });

        var language = (request.LanguageCode ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsSupportedSubmissionLanguage(language))
            return BadRequest(new { message = "Neatbalstīta programmēšanas valoda." });

        var solutionLanguage = (request.SolutionLanguageCode ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsSupportedSubmissionLanguage(solutionLanguage))
            return BadRequest(new { message = "Neatbalstīta risinājuma programmēšanas valoda." });

        if (string.IsNullOrWhiteSpace(request.SolutionCode))
            return BadRequest(new { message = "Autora risinājums nedrīkst būt tukšs." });

        var rawTestCases = request.TestCases?.ToList() ?? [];
        var normalizedAll = rawTestCases
            .Select(tc => (
                Input: NormalizeMultilineText(tc.Input),
                ExpectedOutput: NormalizeMultilineText(tc.ExpectedOutput)))
            .ToList();

        if (normalizedAll.Count < MinimumTotalExerciseTestCases)
        {
            return BadRequest(new
            {
                message =
                    $"Uzdevumam jābūt vismaz {MinimumTotalExerciseTestCases} testiem " +
                    $"(pirmie {SampleTestCaseCount} ir paraugi un ir redzami).",
            });
        }

        if (normalizedAll.Any(tc => string.IsNullOrWhiteSpace(tc.Input) || string.IsNullOrWhiteSpace(tc.ExpectedOutput)))
            return BadRequest(new { message = "Katram testam jābūt ievaddatiem un izvaddatiem." });

        // Pirmie divi testi paliek redzami, pārējie tiek izmantoti slēptajai pārbaudei.
        var normalizedTestCases = normalizedAll
            .Select((tc, index) => new CreateTestCaseRequest(
                tc.Input,
                tc.ExpectedOutput,
                index >= SampleTestCaseCount,
                index))
            .ToList();

        // Autora risinājumam jāiziet visi testi, lai pieprasījumu varētu iesniegt.
        var solutionValidationError = await ValidateReferenceSolutionAsync(
            request.SolutionCode,
            solutionLanguage,
            normalizedTestCases,
            ct);

        if (solutionValidationError is not null)
            return BadRequest(new { message = solutionValidationError });

        // Ja viss ir korekti, uzdevums tiek publicēts tikai pēc administratora apstiprinājuma.
        var pending = new ExerciseRequest
        {
            RequestType = ExerciseRequestType.Create,
            AuthorId = GetCurrentUserId(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Difficulty = difficulty,
            LanguageCode = language,
            LanguageVersion = GetLanguageVersion(language),
            SolutionLanguageCode = solutionLanguage,
            SolutionCode = request.SolutionCode,
            TestCasesJson = JsonSerializer.Serialize(normalizedTestCases),
            Status = ExerciseRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.ExerciseRequests.Add(pending);
        await _db.SaveChangesAsync(ct);

        return Accepted(new { message = "Uzdevums iesniegts apstiprināšanai." });
    }

    // Administratora apstiprināšanas rinda.

    [HttpGet("admin/pending")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<ActionResult<IEnumerable<ExercisePendingDto>>> GetPendingExercises(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var normalizedStatus = (status ?? string.Empty).Trim().ToLowerInvariant();

        var query = _db.ExerciseRequests
            .Include(r => r.Author)
            .AsQueryable();

        query = normalizedStatus switch
        {
            "approved" or "published" => query.Where(r => r.Status == ExerciseRequestStatus.Approved),
            "rejected" => query.Where(r => r.Status == ExerciseRequestStatus.Rejected),
            "" or "all" => query,
            _ => query.Where(r => r.Status == ExerciseRequestStatus.Pending),
        };

        var requests = await query
            .OrderBy(r => r.Status == ExerciseRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(requests.Select(MapPendingExercise));
    }

    [HttpPost("admin/{id:int}/approve")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<ActionResult<ExerciseListItemDto>> ApproveExercise(int id, CancellationToken ct)
    {
        var pending = await _db.ExerciseRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (pending is null)
            return NotFound(new { message = "Pieprasījums nav atrasts." });

        if (pending.Status != ExerciseRequestStatus.Pending)
            return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        if (pending.RequestType == ExerciseRequestType.EditDescription)
        {
            if (pending.ExerciseId is null)
                return BadRequest(new { message = "Pieprasījumam nav piesaistīta uzdevuma." });

            var targetExercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == pending.ExerciseId.Value, ct);
            if (targetExercise is null)
                return NotFound(new { message = "Uzdevums nav atrasts." });

            targetExercise.Description = pending.Description;
            pending.Status = ExerciseRequestStatus.Approved;
            await _db.SaveChangesAsync(ct);

            var testCaseCount = await _db.ExerciseTestCases.CountAsync(tc => tc.ExerciseId == targetExercise.Id, ct);
            return Ok(new ExerciseListItemDto(
                targetExercise.Id,
                targetExercise.Title,
                targetExercise.Description,
                targetExercise.Difficulty,
                targetExercise.LanguageCode,
                targetExercise.LanguageVersion,
                testCaseCount,
                0,
                0,
                0,
                false));
        }

        var testCases = JsonSerializer.Deserialize<List<CreateTestCaseRequest>>(pending.TestCasesJson) ?? [];

        var nextSortOrder = (await _db.Exercises.MaxAsync(e => (int?)e.SortOrder, ct) ?? 0) + 1;
        var exercise = new Exercise
        {
            Title = pending.Title,
            Description = pending.Description,
            Difficulty = pending.Difficulty,
            LanguageCode = pending.LanguageCode,
            LanguageVersion = pending.LanguageVersion,
            SortOrder = nextSortOrder,
            AuthorId = pending.AuthorId,
            CreatedAtUtc = DateTime.UtcNow,
            TestCases = testCases.Select(tc => new ExerciseTestCase
            {
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                IsHidden = tc.IsHidden,
                OrderIndex = tc.OrderIndex,
            }).ToList(),
        };

        _db.Exercises.Add(exercise);
        pending.Status = ExerciseRequestStatus.Approved;
        await _db.SaveChangesAsync(ct);

        return Ok(new ExerciseListItemDto(
            exercise.Id, exercise.Title, exercise.Description, exercise.Difficulty,
            exercise.LanguageCode, exercise.LanguageVersion,
            exercise.TestCases.Count, 0, 0, 0, false));
    }

    [HttpPost("admin/{id:int}/reject")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<IActionResult> RejectExercise(int id, CancellationToken ct)
    {
        var pending = await _db.ExerciseRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (pending is null)
            return NotFound(new { message = "Pieprasījums nav atrasts." });

        if (pending.Status != ExerciseRequestStatus.Pending)
            return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        pending.Status = ExerciseRequestStatus.Rejected;
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("admin/exercises/{id:int}")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<IActionResult> DeleteExercise(int id, CancellationToken ct)
    {
        var exercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exercise is null)
            return NotFound(new { message = "Uzdevums nav atrasts." });

        var pendingRequests = await _db.ExerciseRequests
            .Where(request => request.ExerciseId == id && request.Status == ExerciseRequestStatus.Pending)
            .ToListAsync(ct);

        _db.ExerciseRequests.RemoveRange(pendingRequests);
        _db.Exercises.Remove(exercise);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static ExercisePendingDto MapPendingExercise(ExerciseRequest pending)
    {
        var testCases = ReadPendingTestCases(pending.TestCasesJson);
        return new ExercisePendingDto(
            pending.Id,
            pending.RequestType.ToString(),
            pending.ExerciseId,
            pending.Title,
            pending.Description,
            pending.Difficulty,
            pending.LanguageCode,
            pending.LanguageVersion,
            pending.AuthorId,
            pending.Author?.FullName ?? string.Empty,
            pending.Author?.Email ?? string.Empty,
            pending.CreatedAtUtc,
            pending.Status.ToString(),
            testCases);
    }

    private static IReadOnlyList<ExercisePendingTestCaseDto> ReadPendingTestCases(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var parsed = JsonSerializer.Deserialize<List<CreateTestCaseRequest>>(json) ?? [];
            return parsed
                .Select(tc => new ExercisePendingTestCaseDto(tc.OrderIndex, tc.IsHidden, tc.Input, tc.ExpectedOutput))
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<string?> ValidateReferenceSolutionAsync(
        string solutionCode,
        string solutionLanguage,
        IReadOnlyList<CreateTestCaseRequest> testCases,
        CancellationToken ct)
    {
        var cases = testCases
            .Select((testCase, index) => new ExerciseValidationCase(
                index < SampleTestCaseCount
                    ? $"Tests {index + 1} (paraugs)"
                    : $"Tests {index + 1}",
                testCase.Input,
                testCase.ExpectedOutput))
            .ToList();

        // Visus testus laižam vienā konteinerā, lai amortizētu startēšanas
        // izmaksas. Pirmā kļūda tiek atgriezta kā validācijas ziņojums.
        var runs = await _runner.RunBatchAsync(
            solutionCode,
            cases.Select(c => c.Input).ToList(),
            solutionLanguage,
            onResult: null,
            ct);

        for (var i = 0; i < cases.Count; i++)
        {
            var validationCase = cases[i];
            var run = runs[i];

            if (run.TimedOut)
                return $"{validationCase.Label}: autora risinājums pārsniedza izpildes laiku ({DockerCodeRunnerService.ExecutionTimeout.TotalSeconds:0} s).";

            if (run.ExitCode != 0)
                return $"{validationCase.Label}: autora risinājums beidzās ar kļūdu. {CompactText(run.Stderr)}";

            var actual = run.Stdout.TrimEnd();
            var expected = validationCase.ExpectedOutput.TrimEnd();

            if (actual != expected)
                return $"{validationCase.Label}: autora risinājuma izvade nesakrīt ar gaidīto. Gaidīts: {CompactText(expected)}. Iegūts: {CompactText(actual)}.";
        }

        return null;
    }

    private static string NormalizeMultilineText(string? value) =>
        (value ?? string.Empty).Replace("\r\n", "\n").Replace("\r", "\n").Trim();

    private static string CompactText(string? value)
    {
        var normalized = NormalizeMultilineText(value).Replace("\n", "\\n");
        return normalized.Length <= 240 ? normalized : normalized[..240] + "...";
    }

    private static string? NormalizeDifficulty(string? difficulty)
    {
        var normalized = (difficulty ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "viegls" or "easy" => "viegls",
            "vidējs" or "videjs" or "medium" => "vidējs",
            "grūts" or "gruts" or "hard" => "grūts",
            _ => null,
        };
    }

    private record ExerciseValidationCase(string Label, string Input, string ExpectedOutput);

    private async Task<SubmissionResultDto> ExecuteSubmissionAsync(
        Exercise exercise,
        int userId,
        string code,
        string language,
        Func<SubmissionProgressDto, CancellationToken, Task>? reportProgress,
        CancellationToken ct)
    {
        var alreadySolved = await _db.ExerciseSubmissions
            .AnyAsync(s => s.ExerciseId == exercise.Id && s.UserId == userId
                           && s.Status == SubmissionStatus.Passed, ct);

        var testCases = exercise.TestCases
            .OrderBy(tc => tc.OrderIndex)
            .ToList();

        var submission = new ExerciseSubmission
        {
            ExerciseId = exercise.Id,
            UserId = userId,
            LanguageCode = language,
            Code = code,
            Status = SubmissionStatus.Running,
            TestsTotal = testCases.Count,
            SubmittedAtUtc = DateTime.UtcNow,
        };

        _db.ExerciseSubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);

        if (reportProgress is not null)
        {
            await reportProgress(new SubmissionProgressDto(
                "started",
                submission.Id,
                0,
                testCases.Count,
                0,
                submission.Status.ToString(),
                null,
                null,
                null
            ), ct);
        }

        var testResults = new List<TestCaseResultDto>();
        var passed = 0;
        var sawFailure = false;

        // Visi testi tiek izpildīti vienā Docker konteinerā (RunBatchAsync) — tas
        // amortizē konteinera startēšanas izmaksas. Lai saglabātu sākotnējo
        // lietotāja pieredzi ar apstāšanos pie pirmās kļūdas, ignorējam
        // nākamās progresa atskaites (kods jau ir izpildīts, vienkārši nesaglabājam).
        var inputs = testCases.Select(tc => tc.Input).ToList();

        await _runner.RunBatchAsync(code, inputs, language, async (idx, run, innerCt) =>
        {
            if (sawFailure) return;
            if (idx < 0 || idx >= testCases.Count) return;

            var tc = testCases[idx];
            TestCaseResultDto testResult;

            if (run.TimedOut)
            {
                submission.Status = SubmissionStatus.TimedOut;
                var timeoutMessage = $"Tests pārsniedza izpildes laiku ({DockerCodeRunnerService.ExecutionTimeout.TotalSeconds:0} s).";
                submission.ErrorMessage = timeoutMessage;
                testResult = new TestCaseResultDto(
                    tc.OrderIndex,
                    tc.IsHidden,
                    false,
                    null,
                    tc.IsHidden ? null : tc.ExpectedOutput,
                    timeoutMessage);
                testResults.Add(testResult);
                sawFailure = true;
            }
            else if (run.ExitCode != 0)
            {
                var stderr = run.Stderr.Length > 600 ? run.Stderr[..600] : run.Stderr;
                testResult = new TestCaseResultDto(
                    tc.OrderIndex,
                    tc.IsHidden,
                    false,
                    null,
                    tc.IsHidden ? null : tc.ExpectedOutput,
                    stderr);
                testResults.Add(testResult);
                sawFailure = true;
            }
            else
            {
                var actual = run.Stdout.TrimEnd();
                var expected = tc.ExpectedOutput.TrimEnd();
                var ok = actual == expected;
                if (ok) passed++;
                else sawFailure = true;

                testResult = new TestCaseResultDto(
                    tc.OrderIndex,
                    tc.IsHidden,
                    ok,
                    tc.IsHidden ? null : actual,
                    tc.IsHidden ? null : expected,
                    null
                );
                testResults.Add(testResult);
            }

            await SaveSubmissionProgressAsync(submission, passed, testCases.Count, testResults, innerCt);
            await ReportSubmissionProgressAsync(reportProgress, submission, testResults.Count, testCases.Count, passed, testResult, innerCt);
        }, ct);

        if (submission.Status == SubmissionStatus.Running)
            submission.Status = passed == testCases.Count ? SubmissionStatus.Passed : SubmissionStatus.Failed;

        submission.TestsPassed = passed;
        submission.TestsTotal = testCases.Count;
        submission.TestResultsJson = JsonSerializer.Serialize(testResults);
        submission.ExecutedAtUtc = DateTime.UtcNow;

        var ratingDelta = 0;
        var user = await _db.Users.FindAsync([userId], cancellationToken: ct);
        if (user is not null)
        {
            // Punktus piešķir tikai par pirmo pareizo risinājumu; par kļūdām tos neatņem.
            if (submission.Status == SubmissionStatus.Passed && !alreadySolved)
            {
                ratingDelta = DifficultyPoints(exercise.Difficulty);
                user.Rating += ratingDelta;
            }

            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return new SubmissionResultDto(
            submission.Id,
            submission.Status.ToString(),
            submission.TestsPassed,
            submission.TestsTotal,
            submission.ErrorMessage,
            testResults,
            ratingDelta
        );
    }

    private async Task<Exercise?> FindExerciseWithTestsAsync(int id, CancellationToken ct) =>
        await _db.Exercises
            .Include(e => e.TestCases.OrderBy(tc => tc.OrderIndex))
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    private static string? ValidateSubmissionRequest(SubmitCodeRequest request, out string language)
    {
        language = (request.LanguageCode ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Code))
            return "Kods nedrīkst būt tukšs.";

        return IsSupportedSubmissionLanguage(language)
            ? null
            : "Neatbalstīta programmēšanas valoda.";
    }

    private async Task SaveSubmissionProgressAsync(
        ExerciseSubmission submission,
        int testsPassed,
        int testsTotal,
        IReadOnlyCollection<TestCaseResultDto> testResults,
        CancellationToken ct)
    {
        submission.TestsPassed = testsPassed;
        submission.TestsTotal = testsTotal;
        submission.TestResultsJson = JsonSerializer.Serialize(testResults);
        await _db.SaveChangesAsync(ct);
    }

    private static async Task ReportSubmissionProgressAsync(
        Func<SubmissionProgressDto, CancellationToken, Task>? reportProgress,
        ExerciseSubmission submission,
        int current,
        int total,
        int testsPassed,
        TestCaseResultDto testResult,
        CancellationToken ct)
    {
        if (reportProgress is null)
        {
            return;
        }

        await reportProgress(new SubmissionProgressDto(
            "progress",
            submission.Id,
            current,
            total,
            testsPassed,
            submission.Status.ToString(),
            submission.ErrorMessage,
            testResult,
            null
        ), ct);
    }

    private async Task WriteJsonErrorAsync(int statusCode, string message, CancellationToken ct)
    {
        Response.StatusCode = statusCode;
        Response.ContentType = "application/json; charset=utf-8";
        await Response.WriteAsJsonAsync(new { message }, cancellationToken: ct);
    }

    private static async Task WriteJsonLineAsync(HttpResponse response, SubmissionProgressDto payload, CancellationToken ct)
    {
        await JsonSerializer.SerializeAsync(response.Body, payload, StreamJsonOptions, ct);
        await response.WriteAsync("\n", ct);
        await response.Body.FlushAsync(ct);
    }

    private static ExerciseSubmissionDto MapSubmission(ExerciseSubmission submission) =>
        new(
            submission.Id,
            submission.ExerciseId,
            submission.Exercise.Title,
            submission.Exercise.Difficulty,
            submission.LanguageCode,
            submission.Status.ToString(),
            submission.TestsPassed,
            submission.TestsTotal,
            submission.ErrorMessage,
            DeserializeTestResults(submission.TestResultsJson),
            submission.SubmittedAtUtc,
            submission.ExecutedAtUtc
        );

    private static IReadOnlyList<TestCaseResultDto> DeserializeTestResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TestCaseResultDto>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int DifficultyPoints(string difficulty) =>
        difficulty.ToLowerInvariant() switch
        {
            "viegls" or "easy" => 5,
            "vidējs" or "medium" => 10,
            "grūts" or "hard" => 20,
            _ => 5,
        };

    private static bool IsSupportedSubmissionLanguage(string language) =>
        language is "python" or "java";

    private static string GetLanguageVersion(string language) =>
        language switch
        {
            "java" => "21",
            _ => "3.11",
        };

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : null;
    }
}
