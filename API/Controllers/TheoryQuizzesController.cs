using LearnToCode.API.Contracts.Theory;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LearnToCode.API.Controllers;

[ApiController]
[Route("api/theory/languages/{languageId}/topics/{topicSlug}/quiz")]
public class TheoryQuizzesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TheoryProgressService _progressService;

    public TheoryQuizzesController(AppDbContext db, TheoryProgressService progressService)
    {
        _db = db;
        _progressService = progressService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<TheoryQuizDto>> GetQuiz(
        string languageId, string topicSlug, CancellationToken ct)
    {
        var quiz = await LoadQuizAsync(languageId, topicSlug, ct);
        if (quiz is null)
        {
            return NotFound(new { message = "Tests Åai tÄ“mai nav pieejams." });
        }

        var userId = GetCurrentUserId();
        var questionIds = quiz.Questions.Select(question => question.Id).ToList();
        var answers = userId.HasValue
            ? await _db.TheoryQuizQuestionAnswers
                .Where(a => a.UserId == userId.Value && questionIds.Contains(a.QuestionId))
                .Select(a => new { a.QuestionId, a.IsCorrect, a.SelectedOptionId })
                .ToListAsync(ct)
            : [];

        var answerMap = answers.ToDictionary(answer => answer.QuestionId);

        var dto = new TheoryQuizDto(
            quiz.Id,
            quiz.TopicId,
            GetTopicId(quiz, topicSlug),
            quiz.Topic?.Title ?? string.Empty,
            quiz.Topic?.Language?.Title ?? languageId,
            quiz.Title,
            quiz.Description,
            quiz.Questions.Count,
            answerMap.Count(answer => answer.Value.IsCorrect),
            quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(question =>
                {
                    answerMap.TryGetValue(question.Id, out var answer);
                    var correctOptionId = answer is null
                        ? (int?)null
                        : question.Options.FirstOrDefault(option => option.IsCorrect)?.Id;
                    var selectedOptionId = answer?.SelectedOptionId
                        ?? (answer?.IsCorrect == true ? correctOptionId : null);

                    return new TheoryQuizQuestionDto(
                        question.Id,
                        question.OrderIndex,
                        question.Prompt,
                        question.Explanation,
                        answer is not null,
                        answer?.IsCorrect ?? false,
                        selectedOptionId,
                        correctOptionId,
                        question.Options
                            .OrderBy(o => o.OrderIndex)
                            .Select(option => new TheoryQuizOptionDto(option.Id, option.OrderIndex, option.Text))
                            .ToList());
                })
                .ToList());

        return Ok(dto);
    }

    [HttpGet("manage")]
    [Authorize(Roles = $"{nameof(UserRole.Pedagogs)},{nameof(UserRole.Administrators)}")]
    public async Task<ActionResult<TheoryQuizManagementDto>> GetQuizForManagement(
        string languageId, string topicSlug, CancellationToken ct)
    {
        var quiz = await LoadQuizAsync(languageId, topicSlug, ct);
        if (quiz is null)
        {
            return NotFound(new { message = "Tests Åai tÄ“mai nav pieejams." });
        }

        var dto = new TheoryQuizManagementDto(
            quiz.Id,
            quiz.TopicId,
            GetTopicId(quiz, topicSlug),
            quiz.Topic?.Title ?? string.Empty,
            quiz.Topic?.Language?.Title ?? languageId,
            quiz.Title,
            quiz.Description,
            quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(question =>
                {
                    var orderedOptions = question.Options.OrderBy(option => option.OrderIndex).ToList();
                    var correctIndex = Math.Max(0, orderedOptions.FindIndex(option => option.IsCorrect));

                    return new TheoryQuizManagementQuestionDto(
                        question.Id,
                        question.OrderIndex,
                        question.Prompt,
                        question.Explanation,
                        correctIndex,
                        orderedOptions.Select(option => option.Text).ToList());
                })
                .ToList());

        return Ok(dto);
    }

    [HttpPost("answer")]
    [Authorize]
    public async Task<ActionResult<TheoryQuizAnswerResultDto>> SubmitAnswer(
        string languageId,
        string topicSlug,
        [FromBody] TheoryQuizAnswerRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var quiz = await LoadQuizAsync(languageId, topicSlug, ct);
        if (quiz is null)
        {
            return NotFound(new { message = "Tests Åai tÄ“mai nav pieejams." });
        }

        var question = quiz.Questions.FirstOrDefault(q => q.Id == request.QuestionId);
        if (question is null)
        {
            return BadRequest(new { message = "Å Äds jautÄjums neeksistÄ“ ÅajÄ testÄ." });
        }

        var selectedOption = question.Options.FirstOrDefault(o => o.Id == request.OptionId);
        if (selectedOption is null)
        {
            return BadRequest(new { message = "Å Äda atbilde neeksistÄ“." });
        }

        var correctOption = question.Options.First(o => o.IsCorrect);
        var isCorrect = selectedOption.IsCorrect;

        var existingAnswer = await _db.TheoryQuizQuestionAnswers
            .FirstOrDefaultAsync(a => a.UserId == userId.Value && a.QuestionId == question.Id, ct);

        if (existingAnswer is not null)
        {
            return BadRequest(new { message = "Uz Åo jautÄjumu jau ir atbildÄ“ts." });
        }

        var ratingDelta = 0;
        var ratingAwarded = false;

        _db.TheoryQuizQuestionAnswers.Add(new TheoryQuizQuestionAnswer
        {
            UserId = userId.Value,
            QuestionId = question.Id,
            SelectedOptionId = selectedOption.Id,
            IsCorrect = isCorrect,
            AnsweredAtUtc = DateTime.UtcNow,
        });

        if (isCorrect)
        {
            ratingDelta = 1;
            ratingAwarded = true;
        }

        if (ratingAwarded)
        {
            var user = await _db.Users.FindAsync([userId.Value], cancellationToken: ct);
            if (user is not null)
            {
                user.Rating += ratingDelta;
                user.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Atspoguļo kvīza atbildi tēmas/valodas progresā — invalidē kešu, lai
        // nākamais GetTopic/GetLanguage izsaukums pārrēķina procentus.
        await _progressService.InvalidateUserProgressCacheAsync(userId.Value, ct);

        return Ok(new TheoryQuizAnswerResultDto(
            question.Id,
            selectedOption.Id,
            correctOption.Id,
            isCorrect,
            ratingAwarded,
            ratingDelta,
            question.Explanation));
    }

    [HttpDelete]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<IActionResult> DeleteQuiz(string languageId, string topicSlug, CancellationToken ct)
    {
        var quiz = await LoadQuizAsync(languageId, topicSlug, ct);
        if (quiz is null)
        {
            return NotFound(new { message = "Tests Åai tÄ“mai nav pieejams." });
        }

        _db.TheoryQuizzes.Remove(quiz);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<TheoryQuiz?> LoadQuizAsync(string languageId, string topicSlug, CancellationToken ct)
    {
        var normalizedLanguageId = languageId.Trim().ToLowerInvariant();
        var normalizedTopicId = TheoryTopicKey.Normalize(topicSlug);

        var quizzes = await _db.TheoryQuizzes
            .Include(quiz => quiz.Topic)
                .ThenInclude(topic => topic!.Language)
            .Include(quiz => quiz.Questions.OrderBy(q => q.OrderIndex))
                .ThenInclude(question => question.Options.OrderBy(o => o.OrderIndex))
            .Where(quiz =>
                quiz.Topic != null &&
                quiz.Topic.Language != null &&
                quiz.Topic.Language.Title.ToLower() == normalizedLanguageId)
            .ToListAsync(ct);

        return quizzes.FirstOrDefault(quiz =>
            quiz.Topic != null && TheoryTopicKey.Matches(quiz.Topic.Title, normalizedTopicId));
    }

    private static string GetTopicId(TheoryQuiz quiz, string fallbackTopicId) =>
        quiz.Topic is null ? TheoryTopicKey.Normalize(fallbackTopicId) : TheoryTopicKey.FromTitle(quiz.Topic.Title);

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : null;
    }
}
