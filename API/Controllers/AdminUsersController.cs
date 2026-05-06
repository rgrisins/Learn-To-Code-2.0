using System.Security.Claims;
using LearnToCode.API.Contracts.Auth;
using LearnToCode.API.Services;
using LearnToCode.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Administrators))]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuthSessionService _authSessionService;

    public AdminUsersController(AppDbContext dbContext, IAuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _authSessionService = authSessionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserProfileResponse>>> GetAll(CancellationToken cancellationToken)
    {
        // FullName ir aprēķināma īpašība (FirstName + LastName), tāpēc to nedrīkst
        // lietot tieši vaicājuma Select daļā. Ielādējam
        // entītes un mapojam C# atmiņā.
        var users = await _dbContext.Users
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken);

        return Ok(users.Select(ToResponse).ToList());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserProfileResponse>> Update(int id, [FromBody] AdminUpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Lietotajs nav atrasts." });
        }

        var username = request.Username.Trim();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return BadRequest(new { message = "Lietotajvardam, vardam un uzvardam jabut aizpilditiem." });
        }

        if (username.Length is < 3 or > 30)
        {
            return BadRequest(new { message = "Lietotajvardam jabut 3-30 rakstzimes garam." });
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(new { message = "Nederiga loma." });
        }

        var currentUserId = GetCurrentUserId();
        if (currentUserId == user.Id && role != UserRole.Administrators)
        {
            return BadRequest(new { message = "Nevari nonemt administratora lomu pats sev." });
        }

        var normalizedUsername = username.ToLowerInvariant();
        var usernameTaken = await _dbContext.Users.AnyAsync(
            existingUser => existingUser.Id != user.Id && existingUser.Username == normalizedUsername,
            cancellationToken);

        if (usernameTaken)
        {
            return Conflict(new
            {
                message = "Lietotajvards jau ir aiznemts.",
                errors = new Dictionary<string, string[]>
                {
                    ["username"] = ["Lietotajvards jau ir aiznemts."],
                },
            });
        }

        user.Username = normalizedUsername;
        user.FirstName = firstName;
        user.LastName = lastName;
        user.BirthDate = request.BirthDate;
        user.Role = role;
        user.Rating = Math.Max(0, request.Rating);
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _authSessionService.RefreshUserSessionsAsync(user, cancellationToken);

        return Ok(ToResponse(user));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == id, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Lietotajs nav atrasts." });
        }

        if (GetCurrentUserId() == user.Id)
        {
            return BadRequest(new { message = "Nevari dzest pats savu kontu no admin panela." });
        }

        if (user.Role == UserRole.Administrators)
        {
            var otherAdminExists = await _dbContext.Users.AnyAsync(
                existingUser => existingUser.Id != user.Id && existingUser.Role == UserRole.Administrators,
                cancellationToken);

            if (!otherAdminExists)
            {
                return BadRequest(new { message = "Nevari dzest pedejo administratoru." });
            }
        }

        // Dzēš arī aktīvās sesijas, lai kontu nevarētu turpināt lietot ar vecu JWT.
        await _authSessionService.DeleteUserSessionsAsync(user.Id, cancellationToken);
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("nameid");

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

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
