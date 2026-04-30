using System.Security.Claims;
using System.Text.Json;
using LearnToCode.API.Contracts.Theory;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize(Roles = $"{nameof(UserRole.Pedagogs)},{nameof(UserRole.Administrators)}")]
[Route("api/theory")]
public class TheoryRequestsController : ControllerBase
{
    private static readonly JsonSerializerOptions QuizJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;

    public TheoryRequestsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost("topic-requests")]
    public async Task<ActionResult<TheoryTopicRequestResponse>> CreateTopicRequest(
        [FromBody] CreateTheoryTopicRequestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        if (!Enum.TryParse<TheoryTopicRequestType>(request.RequestType, ignoreCase: true, out var requestType))
            return BadRequest(new { message = "Derīgais tips: 'Add' vai 'Edit'." });

        if (requestType == TheoryTopicRequestType.Add)
        {
            if (string.IsNullOrWhiteSpace(request.ProposedTitle))
                return BadRequest(new { message = "Jaunai tēmai jānorāda virsraksts." });
            if (string.IsNullOrWhiteSpace(request.ProposedDescription))
                return BadRequest(new { message = "Jaunai tēmai jānorāda apraksts." });
            if (string.IsNullOrWhiteSpace(request.ProposedDifficulty))
                return BadRequest(new { message = "Jaunai tēmai jānorāda grūtības pakāpe." });
            if (request.ProposedEstimatedMinutes is null)
                return BadRequest(new { message = "Jaunai tēmai jānorāda aptuvenie minūtes." });
            if (string.IsNullOrWhiteSpace(request.ProposedMarkdown))
                return BadRequest(new { message = "Jaunai tēmai jānorāda sākotnējais saturs." });
        }

        if (requestType != TheoryTopicRequestType.Add && string.IsNullOrWhiteSpace(request.TopicSlug))
        {
            return BadRequest(new { message = "Rediģējamā tēma nav norādīta." });
        }

        var languageCode = request.LanguageCode.Trim().ToLowerInvariant();
        var topicSlug = requestType == TheoryTopicRequestType.Add
            ? await GenerateUniqueTopicSlugAsync(languageCode, request.ProposedTitle!, cancellationToken)
            : request.TopicSlug!.Trim().ToLowerInvariant();

        var now = DateTime.UtcNow;
        var topicRequest = new TheoryTopicRequest
        {
            UserId = userId.Value,
            RequestType = requestType,
            LanguageCode = languageCode,
            TopicSlug = topicSlug,
            ProposedTitle = request.ProposedTitle?.Trim(),
            ProposedDescription = request.ProposedDescription?.Trim(),
            ProposedDifficulty = request.ProposedDifficulty?.Trim(),
            ProposedEstimatedMinutes = request.ProposedEstimatedMinutes,
            ProposedMarkdown = request.ProposedMarkdown?.Trim(),
            Status = TheoryRequestStatus.Pending,
            CreatedAtUtc = now,
        };

        _dbContext.TheoryTopicRequests.Add(topicRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToTopicResponse(topicRequest));
    }

    [HttpGet("topic-requests/my")]
    public async Task<ActionResult<IEnumerable<TheoryTopicRequestResponse>>> GetMyTopicRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        var requests = await _dbContext.TheoryTopicRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(ToTopicResponse));
    }

    [HttpPost("content-requests")]
    public async Task<ActionResult<TheoryContentRequestResponse>> CreateContentRequest(
        [FromBody] CreateTheoryContentRequestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        if (!Enum.TryParse<TheoryContentRequestType>(request.RequestType, ignoreCase: true, out var requestType))
            return BadRequest(new { message = "Derīgais tips: 'Add' vai 'Edit'." });

        if (requestType == TheoryContentRequestType.Edit && request.PageIndex is null)
            return BadRequest(new { message = "Rediģējamai teorijas lapai jānorāda lapas numurs." });

        var now = DateTime.UtcNow;
        var contentRequest = new TheoryContentRequest
        {
            UserId = userId.Value,
            LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(),
            TopicSlug = request.TopicSlug.Trim().ToLowerInvariant(),
            RequestType = requestType,
            PageIndex = request.PageIndex,
            ProposedMarkdown = request.ProposedMarkdown.Trim(),
            Status = TheoryRequestStatus.Pending,
            CreatedAtUtc = now,
        };

        _dbContext.TheoryContentRequests.Add(contentRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToContentResponse(contentRequest));
    }

    [HttpGet("content-requests/my")]
    public async Task<ActionResult<IEnumerable<TheoryContentRequestResponse>>> GetMyContentRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        var requests = await _dbContext.TheoryContentRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(ToContentResponse));
    }

    [HttpPost("quiz-requests")]
    public async Task<ActionResult<TheoryQuizRequestResponse>> CreateQuizRequest(
        [FromBody] CreateTheoryQuizRequestRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        if (!Enum.TryParse<TheoryQuizRequestType>(request.RequestType, ignoreCase: true, out var requestType))
            return BadRequest(new { message = "Derīgais tips: 'Add' vai 'Edit'." });

        var (questions, validationError) = NormalizeQuizQuestions(request.Questions);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var languageCode = request.LanguageCode.Trim().ToLowerInvariant();
        var topicSlug = request.TopicSlug.Trim().ToLowerInvariant();

        var topics = await _dbContext.TheoryTopics
            .Include(t => t.Language)
            .Include(t => t.Quiz)
            .Where(t => t.Language != null && t.Language.Title.ToLower() == languageCode)
            .ToListAsync(cancellationToken);

        var topic = topics.FirstOrDefault(t => TheoryTopicKey.Matches(t.Title, topicSlug));

        if (topic is null)
        {
            return BadRequest(new { message = "Tēma nav atrasta." });
        }

        if (requestType == TheoryQuizRequestType.Add && topic.Quiz is not null)
        {
            return BadRequest(new { message = "Šai tēmai tests jau eksistē. Izmanto testa rediģēšanu." });
        }

        if (requestType == TheoryQuizRequestType.Edit && topic.Quiz is null)
        {
            return BadRequest(new { message = "Šai tēmai vēl nav testa, ko rediģēt." });
        }

        var now = DateTime.UtcNow;
        var quizRequest = new TheoryQuizRequest
        {
            UserId = userId.Value,
            RequestType = requestType,
            LanguageCode = languageCode,
            TopicSlug = topicSlug,
            ProposedTitle = request.ProposedTitle.Trim(),
            ProposedDescription = request.ProposedDescription?.Trim() ?? string.Empty,
            ProposedQuestionsJson = JsonSerializer.Serialize(questions, QuizJsonOptions),
            Status = TheoryRequestStatus.Pending,
            CreatedAtUtc = now,
        };

        _dbContext.TheoryQuizRequests.Add(quizRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToQuizResponse(quizRequest));
    }

    [HttpGet("quiz-requests/my")]
    public async Task<ActionResult<IEnumerable<TheoryQuizRequestResponse>>> GetMyQuizRequests(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "Lietotājs nav autorizēts." });

        var requests = await _dbContext.TheoryQuizRequests
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(ToQuizResponse));
    }

    internal static TheoryTopicRequestResponse ToTopicResponse(TheoryTopicRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Username = r.User?.Username,
        FullName = r.User?.FullName ?? string.Empty,
        Email = r.User?.Email ?? string.Empty,
        RequestType = r.RequestType.ToString(),
        LanguageCode = r.LanguageCode,
        TopicSlug = r.TopicSlug,
        ProposedTitle = r.ProposedTitle,
        ProposedDescription = r.ProposedDescription,
        ProposedDifficulty = r.ProposedDifficulty,
        ProposedEstimatedMinutes = r.ProposedEstimatedMinutes,
        ProposedMarkdown = r.ProposedMarkdown,
        Status = r.Status.ToString(),
        CreatedAtUtc = r.CreatedAtUtc,
    };

    internal static TheoryContentRequestResponse ToContentResponse(TheoryContentRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Username = r.User?.Username,
        FullName = r.User?.FullName ?? string.Empty,
        Email = r.User?.Email ?? string.Empty,
        LanguageCode = r.LanguageCode,
        TopicSlug = r.TopicSlug,
        RequestType = r.RequestType.ToString(),
        PageIndex = r.PageIndex,
        ProposedMarkdown = r.ProposedMarkdown,
        Status = r.Status.ToString(),
        CreatedAtUtc = r.CreatedAtUtc,
    };

    internal static TheoryQuizRequestResponse ToQuizResponse(TheoryQuizRequest r) => new()
    {
        Id = r.Id,
        UserId = r.UserId,
        Username = r.User?.Username,
        FullName = r.User?.FullName ?? string.Empty,
        Email = r.User?.Email ?? string.Empty,
        RequestType = r.RequestType.ToString(),
        LanguageCode = r.LanguageCode,
        TopicSlug = r.TopicSlug,
        ProposedTitle = r.ProposedTitle,
        ProposedDescription = r.ProposedDescription,
        Questions = ReadQuizQuestions(r.ProposedQuestionsJson),
        Status = r.Status.ToString(),
        CreatedAtUtc = r.CreatedAtUtc,
    };

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");
        return int.TryParse(value, out var id) ? id : null;
    }

    private async Task<string> GenerateUniqueTopicSlugAsync(string languageCode, string title, CancellationToken cancellationToken)
    {
        var baseSlug = TheoryTopicKey.FromTitle(title);
        var slug = baseSlug;
        var suffix = 2;

        while (await TopicSlugExistsAsync(languageCode, slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix += 1;
        }

        return slug;
    }

    private async Task<bool> TopicSlugExistsAsync(string languageCode, string slug, CancellationToken cancellationToken)
    {
        var existingTopicTitles = await _dbContext.TheoryTopics
            .AsNoTracking()
            .Where(topic => topic.Language != null && topic.Language.Title.ToLower() == languageCode)
            .Select(topic => topic.Title)
            .ToListAsync(cancellationToken);

        if (existingTopicTitles.Any(title => TheoryTopicKey.Matches(title, slug)))
        {
            return true;
        }

        return await _dbContext.TheoryTopicRequests.AnyAsync(
            request => request.LanguageCode == languageCode &&
                request.TopicSlug == slug &&
                request.RequestType == TheoryTopicRequestType.Add &&
                request.Status == TheoryRequestStatus.Pending,
            cancellationToken);
    }

    private static (List<TheoryQuizQuestionRequestDto> Questions, string? Error) NormalizeQuizQuestions(
        IReadOnlyList<TheoryQuizQuestionRequestDto>? questions)
    {
        if (questions is null || questions.Count == 0)
        {
            return ([], "Testam jāpievieno vismaz viens jautājums.");
        }

        if (questions.Count > 100)
        {
            return ([], "Testā var būt ne vairāk kā 100 jautājumi.");
        }

        var normalized = new List<TheoryQuizQuestionRequestDto>(questions.Count);

        for (var questionIndex = 0; questionIndex < questions.Count; questionIndex += 1)
        {
            var question = questions[questionIndex];
            var prompt = question.Prompt?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return ([], $"Jautājumam Nr. {questionIndex + 1} jānorāda teksts.");
            }

            if (question.Options is null || question.Options.Count != 4)
            {
                return ([], $"Jautājumam Nr. {questionIndex + 1} jābūt tieši 4 atbilžu variantiem.");
            }

            if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex > 3)
            {
                return ([], $"Jautājumam Nr. {questionIndex + 1} jānorāda pareizā atbilde.");
            }

            var options = new List<string>(4);
            for (var optionIndex = 0; optionIndex < question.Options.Count; optionIndex += 1)
            {
                var option = question.Options[optionIndex]?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(option))
                {
                    return ([], $"Jautājuma Nr. {questionIndex + 1} atbilde Nr. {optionIndex + 1} nevar būt tukša.");
                }

                if (option.Length > 500)
                {
                    return ([], $"Jautājuma Nr. {questionIndex + 1} atbilde Nr. {optionIndex + 1} ir pārāk gara.");
                }

                options.Add(option);
            }

            normalized.Add(new TheoryQuizQuestionRequestDto
            {
                Prompt = prompt,
                Explanation = string.IsNullOrWhiteSpace(question.Explanation) ? null : question.Explanation.Trim(),
                Options = options,
                CorrectOptionIndex = question.CorrectOptionIndex,
            });
        }

        return (normalized, null);
    }

    internal static IReadOnlyList<TheoryQuizQuestionRequestDto> ReadQuizQuestions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<TheoryQuizQuestionRequestDto>>(json, QuizJsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
