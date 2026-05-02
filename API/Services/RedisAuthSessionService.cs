using System.Text.Json;
using LearnToCode.API.Contracts.Auth;
using LearnToCode.Data;
using StackExchange.Redis;

namespace LearnToCode.API.Services;

public interface IAuthSessionService
{
    Task<AuthSessionRecord> CreateSessionAsync(string sessionId, User user, TokenGenerationResult tokenResult, string token, CancellationToken cancellationToken);

    Task<AuthSessionRecord?> UpdateAccessTokenAsync(string sessionId, TokenGenerationResult tokenResult, CancellationToken cancellationToken);

    Task<AuthSessionRecord?> GetSessionAsync(string sessionId, CancellationToken cancellationToken);

    Task<bool> ValidateSessionAsync(string sessionId, string jwtId, string token, CancellationToken cancellationToken);

    Task<bool> ValidateOrRecreateSessionAsync(string sessionId, string jwtId, string token, User user, TokenGenerationResult tokenResult, CancellationToken cancellationToken);

    Task<AuthSessionRecord?> UpdateUserStateAsync(string sessionId, User user, CancellationToken cancellationToken);

    Task RefreshUserSessionsAsync(User user, CancellationToken cancellationToken);

    Task DeleteUserSessionsAsync(int userId, CancellationToken cancellationToken);

    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken);
}

public sealed class RedisAuthSessionService : IAuthSessionService
{
    private const string SessionKeyPrefix = "learn-to-code:auth:session:";
    private const string UserSessionsKeyPrefix = "learn-to-code:auth:user-sessions:";

    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;

    public RedisAuthSessionService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        _configuration = configuration;
    }

    private TimeSpan GetRefreshTokenLifetime()
    {
        if (int.TryParse(_configuration["Auth:RefreshTokenDays"], out var refreshTokenDays) && refreshTokenDays > 0)
        {
            return TimeSpan.FromDays(refreshTokenDays);
        }

        return TimeSpan.FromDays(7);
    }

    private int GetDatabaseIndex()
    {
        return int.TryParse(_configuration["Redis:Database"], out var database) ? database : 0;
    }

    public async Task<AuthSessionRecord> CreateSessionAsync(string sessionId, User user, TokenGenerationResult tokenResult, string token, CancellationToken cancellationToken)
    {
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(GetRefreshTokenLifetime());
        var expiresAtUtc = tokenResult.ExpiresAtUtc > refreshTokenExpiresAtUtc ? tokenResult.ExpiresAtUtc : refreshTokenExpiresAtUtc;

        var record = new AuthSessionRecord
        {
            SessionId = sessionId,
            UserId = user.Id,
            User = ToUserProfileResponse(user),
            Email = user.Email,
            Role = user.Role.ToString(),
            AccessToken = token,
            JwtId = tokenResult.JwtId,
            AccessTokenExpiresAtUtc = tokenResult.ExpiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc,
        };

        var database = _redis.GetDatabase(GetDatabaseIndex());
        var key = GetSessionKey(record.SessionId);
        var payload = JsonSerializer.Serialize(record);
        var ttl = record.ExpiresAtUtc - DateTime.UtcNow;

        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromMinutes(1);
        }

        await database.StringSetAsync(key, payload, ttl).WaitAsync(cancellationToken);
        await database.SetAddAsync(GetUserSessionsKey(user.Id), record.SessionId).WaitAsync(cancellationToken);
        await database.KeyExpireAsync(GetUserSessionsKey(user.Id), ttl).WaitAsync(cancellationToken);

        return record;
    }

    public async Task<AuthSessionRecord?> UpdateAccessTokenAsync(string sessionId, TokenGenerationResult tokenResult, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var key = GetSessionKey(sessionId);
        var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (!value.HasValue)
        {
            return null;
        }

        var record = JsonSerializer.Deserialize<AuthSessionRecord>(value.ToString());
        if (record is null || record.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        record.AccessToken = tokenResult.Token;
        record.JwtId = tokenResult.JwtId;
        record.AccessTokenExpiresAtUtc = tokenResult.ExpiresAtUtc;

        var ttl = record.ExpiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return null;
        }

        await database.StringSetAsync(key, JsonSerializer.Serialize(record), ttl).WaitAsync(cancellationToken);

        return record;
    }

    public async Task<AuthSessionRecord?> GetSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var value = await database.StringGetAsync(GetSessionKey(sessionId)).WaitAsync(cancellationToken);
        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<AuthSessionRecord>(value.ToString());
    }

    public async Task<bool> ValidateSessionAsync(string sessionId, string jwtId, string token, CancellationToken cancellationToken)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return false;
        }

        if (!string.Equals(session.JwtId, jwtId, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(session.AccessToken, token, StringComparison.Ordinal))
        {
            return false;
        }

        return session.ExpiresAtUtc > DateTime.UtcNow && session.AccessTokenExpiresAtUtc > DateTime.UtcNow;
    }

    public async Task<bool> ValidateOrRecreateSessionAsync(string sessionId, string jwtId, string token, User user, TokenGenerationResult tokenResult, CancellationToken cancellationToken)
    {
        // First, try to validate the existing session
        var isValid = await ValidateSessionAsync(sessionId, jwtId, token, cancellationToken);
        if (isValid)
        {
            return true;
        }

        // If validation fails, the session might have expired in Redis
        // Recreate it from the valid JWT claims if the token hasn't expired yet
        if (tokenResult.ExpiresAtUtc > DateTime.UtcNow)
        {
            await CreateSessionAsync(sessionId, user, tokenResult, token, cancellationToken);
            return true;
        }

        // Token itself has expired
        return false;
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var key = GetSessionKey(sessionId);
        var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (value.HasValue)
        {
            var record = JsonSerializer.Deserialize<AuthSessionRecord>(value.ToString());
            if (record is not null)
            {
                await database.SetRemoveAsync(GetUserSessionsKey(record.UserId), sessionId).WaitAsync(cancellationToken);
            }
        }

        await database.KeyDeleteAsync(key).WaitAsync(cancellationToken);
    }

    public async Task<AuthSessionRecord?> UpdateUserStateAsync(string sessionId, User user, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var key = GetSessionKey(sessionId);
        var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (!value.HasValue)
        {
            return null;
        }

        var record = JsonSerializer.Deserialize<AuthSessionRecord>(value.ToString());
        if (record is null || record.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return null;
        }

        ApplyUserState(record, user);

        var ttl = record.ExpiresAtUtc - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return null;
        }

        await database.StringSetAsync(key, JsonSerializer.Serialize(record), ttl).WaitAsync(cancellationToken);
        await database.SetAddAsync(GetUserSessionsKey(user.Id), sessionId).WaitAsync(cancellationToken);
        await database.KeyExpireAsync(GetUserSessionsKey(user.Id), ttl).WaitAsync(cancellationToken);
        return record;
    }

    public async Task RefreshUserSessionsAsync(User user, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var sessionsKey = GetUserSessionsKey(user.Id);
        var sessionIds = await database.SetMembersAsync(sessionsKey).WaitAsync(cancellationToken);

        foreach (var redisValue in sessionIds)
        {
            var sessionId = redisValue.ToString();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                continue;
            }

            var updated = await UpdateUserStateAsync(sessionId, user, cancellationToken);
            if (updated is null)
            {
                await database.SetRemoveAsync(sessionsKey, sessionId).WaitAsync(cancellationToken);
            }
        }
    }

    public async Task DeleteUserSessionsAsync(int userId, CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase(GetDatabaseIndex());
        var sessionsKey = GetUserSessionsKey(userId);
        var sessionIds = await database.SetMembersAsync(sessionsKey).WaitAsync(cancellationToken);

        foreach (var redisValue in sessionIds)
        {
            var sessionId = redisValue.ToString();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await database.KeyDeleteAsync(GetSessionKey(sessionId)).WaitAsync(cancellationToken);
            }
        }

        await database.KeyDeleteAsync(sessionsKey).WaitAsync(cancellationToken);
    }

    private static string GetSessionKey(string sessionId) => $"{SessionKeyPrefix}{sessionId}";

    private static string GetUserSessionsKey(int userId) => $"{UserSessionsKeyPrefix}{userId}";

    private static void ApplyUserState(AuthSessionRecord record, User user)
    {
        record.UserId = user.Id;
        record.User = ToUserProfileResponse(user);
        record.Email = user.Email;
        record.Role = user.Role.ToString();
    }

    private static UserProfileResponse ToUserProfileResponse(User user)
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

public sealed class AuthSessionRecord
{
    public string SessionId { get; set; } = string.Empty;

    public int UserId { get; set; }

    public UserProfileResponse User { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public string JwtId { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
