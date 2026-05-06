using System.Security.Claims;
using LearnToCode.API.Contracts.Auth;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthSessionService _authSessionService;
    private readonly TheoryProgressService _theoryProgressService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ProfileController(
        AppDbContext dbContext,
        IAuthSessionService authSessionService,
        TheoryProgressService theoryProgressService,
        IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
        _theoryProgressService = theoryProgressService;
        _passwordHasher = passwordHasher;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("sid");
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await _authSessionService.GetSessionAsync(sessionId, cancellationToken);
            if (session?.User.Id > 0)
            {
                return Ok(session.User);
            }
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _authSessionService.UpdateUserStateAsync(sessionId, user, cancellationToken);
        }

        return Ok(ToResponse(user));
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileResponse>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        // Lietotājvārds datubāzē jau ir ar mazajiem burtiem un bez liekām atstarpēm.
        user.Username = request.Username.Trim().ToLowerInvariant();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.BirthDate = request.BirthDate;
        user.Representation = string.IsNullOrWhiteSpace(request.Representation)
            ? null
            : request.Representation.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        if (user.Bio is not null && user.Bio.Length > 500)
        {
            return BadRequest(new { message = "Apraksts nedrīkst pārsniegt 500 rakstzīmes." });
        }

        // Paroles maiņa notiek tikai tad, ja iesniegta pašreizējā un jaunā parole.
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return BadRequest(new { message = "Norādi pašreizējo paroli." });
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (verification == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { message = "Pašreizējā parole ir nepareiza." });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Jaunajai parolei jābūt vismaz 6 rakstzīmes garai." });
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var sessionId = User.FindFirstValue("sid");
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _authSessionService.UpdateUserStateAsync(sessionId, user, cancellationToken);
        }

        return Ok(ToResponse(user));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProfileStatsResponse>> Stats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await BuildStatsAsync(userId.Value, cancellationToken));
    }

    [HttpGet("users/{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicUserProfileResponse>> PublicProfile(string username, CancellationToken cancellationToken)
    {
        var normalized = username.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized))
        {
            return NotFound(new { message = "Lietotājs nav atrasts." });
        }

        // Vēsturiski daži ieraksti DB var saturēt jaukto reģistru lietotājvārdus,
        // tāpēc salīdzinām caur LOWER() un nepaļaujamies uz reģistrjutīgu ==.
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Username != null && item.Username.ToLower() == normalized,
                cancellationToken);

        if (user is null)
        {
            return NotFound(new { message = "Lietotājs nav atrasts." });
        }

        return Ok(new PublicUserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            Representation = user.Representation,
            Bio = user.Bio,
            BirthDate = user.BirthDate,
            Rating = user.Rating,
            CreatedAtUtc = user.CreatedAtUtc,
            Stats = await BuildStatsAsync(user.Id, cancellationToken),
        });
    }

    private async Task<ProfileStatsResponse> BuildStatsAsync(int userId, CancellationToken cancellationToken)
    {
        var languageProgress = await _theoryProgressService.GetLanguageProgressPercentsAsync(userId, cancellationToken);
        var languages = await _dbContext.TheoryLanguages
            .AsNoTracking()
            .OrderBy(language => language.SortOrder)
            .ThenBy(language => language.Title)
            .Select(language => new
            {
                language.Title,
                TopicCount = language.Topics.Count(topic => topic.Content != null),
            })
            .ToListAsync(cancellationToken);

        var theoryLanguages = languages
            .Select(language => new ProfileTheoryLanguageProgressResponse
            {
                LanguageId = language.Title,
                Title = language.Title,
                TopicCount = language.TopicCount,
                ProgressPercent = languageProgress.GetValueOrDefault(language.Title),
            })
            .ToList();

        if (theoryLanguages.Count == 0)
        {
            theoryLanguages.Add(new ProfileTheoryLanguageProgressResponse
            {
                LanguageId = "Python",
                Title = "Python",
                TopicCount = 0,
                ProgressPercent = 0,
            });
        }

        var exerciseTotal = await _dbContext.Exercises
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var submissions = await _dbContext.ExerciseSubmissions
            .AsNoTracking()
            .Where(submission => submission.UserId == userId)
            .Select(submission => new
            {
                submission.ExerciseId,
                submission.LanguageCode,
                submission.Status,
            })
            .ToListAsync(cancellationToken);

        var mostUsedLanguage = submissions
            .GroupBy(submission => submission.LanguageCode.Trim().ToLowerInvariant())
            .Select(group => new
            {
                LanguageCode = group.Key,
                SubmissionCount = group.Count(),
            })
            .OrderByDescending(language => language.SubmissionCount)
            .ThenBy(language => language.LanguageCode)
            .FirstOrDefault();

        var passedSubmissions = submissions.Count(submission => submission.Status == SubmissionStatus.Passed);
        var failedSubmissions = submissions.Count(submission =>
            submission.Status is SubmissionStatus.Failed or SubmissionStatus.Error or SubmissionStatus.TimedOut);
        var completedSubmissions = passedSubmissions + failedSubmissions;
        var solvedExercises = submissions
            .Where(submission => submission.Status == SubmissionStatus.Passed)
            .Select(submission => submission.ExerciseId)
            .Distinct()
            .Count();

        return new ProfileStatsResponse
        {
            TheoryLanguages = theoryLanguages,
            ExerciseTotal = exerciseTotal,
            ExerciseAttempted = submissions.Select(submission => submission.ExerciseId).Distinct().Count(),
            ExerciseSolved = solvedExercises,
            ExerciseSubmissionCount = submissions.Count,
            ExercisePassedSubmissions = passedSubmissions,
            ExerciseFailedSubmissions = failedSubmissions,
            ExerciseCompletionPercent = CalculatePercent(solvedExercises, exerciseTotal),
            ExerciseSuccessPercent = CalculatePercent(passedSubmissions, completedSubmissions),
            MostUsedExerciseLanguage = mostUsedLanguage?.LanguageCode,
            MostUsedExerciseLanguageSubmissionCount = mostUsedLanguage?.SubmissionCount ?? 0,
        };
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId.Value, cancellationToken);
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return userId;
    }

    private static int CalculatePercent(int value, int total) =>
        total <= 0 ? 0 : (int)Math.Round(value * 100.0 / total);

    private static UserProfileResponse ToResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            Representation = user.Representation,
            Bio = user.Bio,
            Rating = user.Rating,
            CreatedAtUtc = user.CreatedAtUtc,
        };
    }
}
