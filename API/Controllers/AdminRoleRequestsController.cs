using LearnToCode.API.Contracts.Auth;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Administrators))]
[Route("api/admin/role-requests")]
public class AdminRoleRequestsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthSessionService _authSessionService;

    public AdminRoleRequestsController(AppDbContext dbContext, IAuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleRequestResponse>>> GetAll([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.RoleRequests
            .Include(request => request.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RoleRequestStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(request => request.Status == parsedStatus);
        }

        var requests = await query
            .OrderBy(request => request.Status == RoleRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(requests.Select(RoleRequestMapper.ToResponse));
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<RoleRequestResponse>> Approve(int id, CancellationToken cancellationToken)
    {
        var roleRequest = await FindRequestAsync(id, cancellationToken);
        if (roleRequest is null)
        {
            return NotFound(new { message = "Pieprasijums nav atrasts." });
        }

        if (roleRequest.Status != RoleRequestStatus.Pending)
        {
            return BadRequest(new { message = "Pieprasijums jau ir izskatits." });
        }

        if (roleRequest.User is null)
        {
            return BadRequest(new { message = "Pieprasijuma lietotajs nav atrasts." });
        }

        roleRequest.User.Role = roleRequest.RequestedRole;
        roleRequest.User.UpdatedAtUtc = DateTime.UtcNow;
        roleRequest.Status = RoleRequestStatus.Approved;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authSessionService.RefreshUserSessionsAsync(roleRequest.User, cancellationToken);

        return Ok(RoleRequestMapper.ToResponse(roleRequest));
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<RoleRequestResponse>> Reject(int id, CancellationToken cancellationToken)
    {
        var roleRequest = await FindRequestAsync(id, cancellationToken);
        if (roleRequest is null)
        {
            return NotFound(new { message = "Pieprasijums nav atrasts." });
        }

        if (roleRequest.Status != RoleRequestStatus.Pending)
        {
            return BadRequest(new { message = "Pieprasijums jau ir izskatits." });
        }

        roleRequest.Status = RoleRequestStatus.Rejected;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(RoleRequestMapper.ToResponse(roleRequest));
    }

    private Task<RoleRequest?> FindRequestAsync(int id, CancellationToken cancellationToken)
    {
        return _dbContext.RoleRequests
            .Include(request => request.User)
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken);
    }
}
