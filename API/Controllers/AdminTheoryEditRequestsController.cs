using System.Security.Claims;
using LearnToCode.API.Contracts.Theory;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Administrators))]
[Route("api/admin/theory-requests")]
public class AdminTheoryRequestsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly TheoryCatalogService _catalogService;

    public AdminTheoryRequestsController(AppDbContext dbContext, TheoryCatalogService catalogService)
    {
        _dbContext = dbContext;
        _catalogService = catalogService;
    }

    // Tēmu pieprasījumu apstrāde.

    [HttpGet("topics")]
    public async Task<ActionResult<IEnumerable<TheoryTopicRequestResponse>>> GetTopicRequests(
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.TheoryTopicRequests.Include(r => r.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TheoryRequestStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        var list = await query
            .OrderBy(r => r.Status == TheoryRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(TheoryRequestsController.ToTopicResponse));
    }

    [HttpPost("topics/{id:int}/approve")]
    public async Task<ActionResult<TheoryTopicRequestResponse>> ApproveTopicRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryTopicRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        var (success, error) = await _catalogService.ApplyTopicRequestAsync(request, cancellationToken);
        if (!success) return BadRequest(new { message = error ?? "Neizdevās lietot izmaiņas." });

        request.Status = TheoryRequestStatus.Approved;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToTopicResponse(request));
    }

    [HttpPost("topics/{id:int}/reject")]
    public async Task<ActionResult<TheoryTopicRequestResponse>> RejectTopicRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryTopicRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        request.Status = TheoryRequestStatus.Rejected;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToTopicResponse(request));
    }

    // Teorijas satura pieprasījumu apstrāde.

    [HttpGet("content")]
    public async Task<ActionResult<IEnumerable<TheoryContentRequestResponse>>> GetContentRequests(
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.TheoryContentRequests.Include(r => r.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TheoryRequestStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        var list = await query
            .OrderBy(r => r.Status == TheoryRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(TheoryRequestsController.ToContentResponse));
    }

    [HttpPost("content/{id:int}/approve")]
    public async Task<ActionResult<TheoryContentRequestResponse>> ApproveContentRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryContentRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        var (success, error) = await _catalogService.ApplyContentRequestAsync(request, cancellationToken);
        if (!success) return BadRequest(new { message = error ?? "Neizdevās lietot izmaiņas." });

        request.Status = TheoryRequestStatus.Approved;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToContentResponse(request));
    }

    [HttpPost("content/{id:int}/reject")]
    public async Task<ActionResult<TheoryContentRequestResponse>> RejectContentRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryContentRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        request.Status = TheoryRequestStatus.Rejected;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToContentResponse(request));
    }

    // Teorijas testu pieprasījumu apstrāde.

    [HttpGet("quizzes")]
    public async Task<ActionResult<IEnumerable<TheoryQuizRequestResponse>>> GetQuizRequests(
        [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.TheoryQuizRequests.Include(r => r.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TheoryRequestStatus>(status, ignoreCase: true, out var parsed))
        {
            query = query.Where(r => r.Status == parsed);
        }

        var list = await query
            .OrderBy(r => r.Status == TheoryRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(TheoryRequestsController.ToQuizResponse));
    }

    [HttpPost("quizzes/{id:int}/approve")]
    public async Task<ActionResult<TheoryQuizRequestResponse>> ApproveQuizRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryQuizRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        var (success, error) = await _catalogService.ApplyQuizRequestAsync(request, cancellationToken);
        if (!success) return BadRequest(new { message = error ?? "Neizdevās lietot izmaiņas." });

        request.Status = TheoryRequestStatus.Approved;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToQuizResponse(request));
    }

    [HttpPost("quizzes/{id:int}/reject")]
    public async Task<ActionResult<TheoryQuizRequestResponse>> RejectQuizRequest(
        int id, CancellationToken cancellationToken)
    {
        var request = await _dbContext.TheoryQuizRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null) return NotFound(new { message = "Pieprasījums nav atrasts." });
        if (request.Status != TheoryRequestStatus.Pending) return BadRequest(new { message = "Pieprasījums jau ir izskatīts." });

        request.Status = TheoryRequestStatus.Rejected;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(TheoryRequestsController.ToQuizResponse(request));
    }

}
