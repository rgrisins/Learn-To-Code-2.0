using System.Security.Claims;
using LearnToCode.API.Contracts.Auth;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile/role-requests")]
public class ProfileRoleRequestsController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ProfileRoleRequestsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleRequestResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var requests = await _dbContext.RoleRequests
            .Include(request => request.User)
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(RoleRequestMapper.ToResponse));
    }

    [HttpPost]
    public async Task<ActionResult<RoleRequestResponse>> Create([FromBody] CreateRoleRequestRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<UserRole>(request.RequestedRole, ignoreCase: true, out var requestedRole) ||
            requestedRole != UserRole.Pedagogs)
        {
            return BadRequest(new { message = "Paslaik var pieprasit tikai pedagoga lomu." });
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 10)
        {
            return BadRequest(new { message = "Iemeslam jabut vismaz 10 rakstzimes garam." });
        }

        if (reason.Length > 1000)
        {
            return BadRequest(new { message = "Iemesls ir par garu." });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (user.Role == UserRole.Pedagogs || user.Role == UserRole.Administrators)
        {
            return BadRequest(new { message = "Tev jau ir augstaka loma." });
        }

        var hasPendingRequest = await _dbContext.RoleRequests.AnyAsync(
            existingRequest => existingRequest.UserId == user.Id &&
                existingRequest.RequestedRole == requestedRole &&
                existingRequest.Status == RoleRequestStatus.Pending,
            cancellationToken);

        if (hasPendingRequest)
        {
            return Conflict(new { message = "Tev jau ir aktīvs pedagoga lomas pieprasijums." });
        }

        var roleRequest = new RoleRequest
        {
            UserId = user.Id,
            User = user,
            RequestedRole = requestedRole,
            Reason = reason,
            Status = RoleRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };

        _dbContext.RoleRequests.Add(roleRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(RoleRequestMapper.ToResponse(roleRequest));
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
