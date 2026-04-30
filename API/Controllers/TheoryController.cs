using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearnToCode.API.Controllers;

[ApiController]
[Route("api/theory")]
public class TheoryController : ControllerBase
{
    private readonly TheoryCatalogService _catalogService;

    public TheoryController(TheoryCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("languages")]
    public async Task<IActionResult> GetLanguages(CancellationToken cancellationToken)
    {
        return Ok(await _catalogService.GetLanguagesAsync(GetCurrentUserId(), cancellationToken));
    }

    [HttpGet("languages/{languageId}")]
    public async Task<IActionResult> GetLanguage(string languageId, CancellationToken cancellationToken)
    {
        var language = await _catalogService.GetLanguageAsync(languageId, GetCurrentUserId(), cancellationToken);
        return language is null ? NotFound(new { message = "Valoda nav atrasta." }) : Ok(language);
    }

    [HttpGet("languages/{languageId}/topics/{topicId}/pages/{page:int}")]
    public async Task<IActionResult> GetPage(string languageId, string topicId, int page, CancellationToken cancellationToken)
    {
        var theoryPage = await _catalogService.GetPageAsync(languageId, topicId, page, GetCurrentUserId(), cancellationToken);
        return theoryPage is null ? NotFound(new { message = "Teorijas lapa nav atrasta." }) : Ok(theoryPage);
    }

    [HttpGet("languages/{languageId}/topics/{topicId}/read-pages")]
    [Authorize]
    public async Task<IActionResult> GetReadPages(string languageId, string topicId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var indices = await _catalogService.GetReadPageIndicesAsync(userId.Value, languageId, topicId, cancellationToken);
        return Ok(indices);
    }

    [HttpPost("languages/{languageId}/topics/{topicId}/pages/{page:int}/read")]
    [Authorize]
    public async Task<IActionResult> MarkPageRead(string languageId, string topicId, int page, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var progress = await _catalogService.MarkPageReadAsync(userId.Value, languageId, topicId, page, cancellationToken);
        return progress is null ? NotFound(new { message = "Teorijas lapa nav atrasta." }) : Ok(progress);
    }

    [HttpDelete("languages/{languageId}/topics/{topicId}")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<IActionResult> DeleteTopic(string languageId, string topicId, CancellationToken cancellationToken)
    {
        var (success, error) = await _catalogService.DeleteTopicAsync(languageId, topicId, cancellationToken);
        return success ? NoContent() : NotFound(new { message = error ?? "Tēma nav atrasta." });
    }

    [HttpDelete("languages/{languageId}/topics/{topicId}/pages/{page:int}")]
    [Authorize(Roles = nameof(UserRole.Administrators))]
    public async Task<IActionResult> DeletePage(string languageId, string topicId, int page, CancellationToken cancellationToken)
    {
        var (success, error) = await _catalogService.DeleteContentPageAsync(languageId, topicId, page, cancellationToken);
        if (success)
        {
            return NoContent();
        }

        return error?.Contains("pēdējo", StringComparison.OrdinalIgnoreCase) == true
            ? BadRequest(new { message = error })
            : NotFound(new { message = error ?? "Teorijas lapa nav atrasta." });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(value, out var id) ? id : null;
    }
}
