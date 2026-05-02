using LearnToCode.API.Contracts.Auth;
using LearnToCode.API.Services;
using LearnToCode.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnToCode.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IAuthSessionService _authSessionService;

    public AuthController(AppDbContext dbContext, IPasswordHasher<User> passwordHasher, ITokenService tokenService, IAuthSessionService authSessionService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _authSessionService = authSessionService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Aizpildi lietotājvārdu, vārdu, uzvārdu un paroli." });
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return BadRequest(new { message = "Nederīga loma." });
        }

        if (role == UserRole.Administrators)
        {
            return BadRequest(new { message = "Administrators nav pieejams pašreģistrācijai." });
        }

        var requestedRole = role;
        var roleRequestReason = request.RoleRequestReason?.Trim();

        if (requestedRole == UserRole.Pedagogs && (string.IsNullOrWhiteSpace(roleRequestReason) || roleRequestReason.Length < 10))
        {
            return BadRequest(new { message = "Pedagoga lomas pieprasijumam janorada vismaz 10 rakstzimes garu iemeslu." });
        }

        if (roleRequestReason?.Length > 1000)
        {
            return BadRequest(new { message = "Pedagoga lomas pieprasijuma iemesls ir par garu." });
        }

        // Username un Email DB jau ir lowercase + trim, salīdzinām attiecīgi.
        var normalizedUsername = username.ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email || user.Username == normalizedUsername, cancellationToken);

        if (existingUser is not null)
        {
            var errors = new Dictionary<string, string[]>();

            if (existingUser.Username == normalizedUsername)
            {
                errors["username"] = ["Lietotājvārds jau ir aizņemts."];
            }

            if (existingUser.Email == email)
            {
                errors["email"] = ["E-pasts jau ir aizņemts."];
            }

            return Conflict(new
            {
                message = "Daži lauki jau ir izmantoti.",
                errors,
            });
        }

        var user = new User
        {
            Username = normalizedUsername,
            FirstName = firstName,
            LastName = lastName,
            BirthDate = request.BirthDate,
            Email = email,
            Representation = string.IsNullOrWhiteSpace(request.Representation) ? null : request.Representation.Trim(),
            Rating = 1000,
            Role = requestedRole == UserRole.Pedagogs ? UserRole.Audzeknis : requestedRole,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (requestedRole == UserRole.Pedagogs)
        {
            _dbContext.RoleRequests.Add(new RoleRequest
            {
                UserId = user.Id,
                RequestedRole = UserRole.Pedagogs,
                Reason = roleRequestReason!,
                Status = RoleRequestStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return Ok(await CreateAuthResponseAsync(user, cancellationToken));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var usernameOrEmail = request.Username.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(existingUser => existingUser.Username == usernameOrEmail || existingUser.Email == usernameOrEmail, cancellationToken);

        if (user is null)
        {
            return Unauthorized(new { message = "Nepareizs lietotājvārds vai parole." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Nepareizs lietotājvārds vai parole." });
        }

        return Ok(await CreateAuthResponseAsync(user, cancellationToken));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var sessionId = User.FindFirstValue("sid");
        if (string.IsNullOrWhiteSpace(sessionId) && Request.Cookies.TryGetValue(AuthCookieDefaults.RefreshTokenCookieName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
        {
            sessionId = refreshToken;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(Request));
            return NoContent();
        }

        await _authSessionService.DeleteSessionAsync(sessionId, cancellationToken);
        Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(Request));
        return NoContent();
    }

    [HttpGet("session")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GetSession(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookieDefaults.RefreshTokenCookieName, out var sessionId) || string.IsNullOrWhiteSpace(sessionId))
        {
            return Unauthorized();
        }

        var session = await _authSessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session is null || session.ExpiresAtUtc <= DateTime.UtcNow)
        {
            Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(Request));
            return Unauthorized();
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(existingUser => existingUser.Id == session.UserId, cancellationToken);

        if (user is null)
        {
            await _authSessionService.DeleteSessionAsync(sessionId, cancellationToken);
            Response.Cookies.Delete(AuthCookieDefaults.RefreshTokenCookieName, AuthCookieDefaults.CreateDeletionCookieOptions(Request));
            return Unauthorized();
        }

        if (session.AccessTokenExpiresAtUtc <= DateTime.UtcNow || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            var tokenResult = _tokenService.CreateToken(user, sessionId);
            session = await _authSessionService.UpdateAccessTokenAsync(sessionId, tokenResult, cancellationToken)
                ?? await _authSessionService.CreateSessionAsync(sessionId, user, tokenResult, tokenResult.Token, cancellationToken);
        }

        return Ok(new AuthResponse
        {
            User = CreateUserProfileResponse(user),
            AccessToken = session.AccessToken,
            AccessTokenExpiresAtUtc = session.AccessTokenExpiresAtUtc,
        });
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var tokenResult = _tokenService.CreateToken(user, sessionId);
        var session = await _authSessionService.CreateSessionAsync(sessionId, user, tokenResult, tokenResult.Token, cancellationToken);
        Response.Cookies.Append(
            AuthCookieDefaults.RefreshTokenCookieName,
            sessionId,
            AuthCookieDefaults.CreateCookieOptions(Request, new DateTimeOffset(session.ExpiresAtUtc)));

        return new AuthResponse
        {
            User = CreateUserProfileResponse(user),
            AccessToken = session.AccessToken,
            AccessTokenExpiresAtUtc = session.AccessTokenExpiresAtUtc,
        };
    }

    private static UserProfileResponse CreateUserProfileResponse(User user)
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
