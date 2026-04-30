using Microsoft.AspNetCore.Http;

namespace LearnToCode.API.Services;

public static class AuthCookieDefaults
{
    public const string RefreshTokenCookieName = "ltc.auth.refresh";

    public static CookieOptions CreateCookieOptions(HttpRequest request, DateTimeOffset expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = IsBrowserHttps(request),
        SameSite = ShouldUseCrossSiteCookie(request) && IsBrowserHttps(request) ? SameSiteMode.None : SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
        Domain = null, // Allow cookie to be sent to proxied requests
        Expires = expiresAtUtc,
    };

    public static CookieOptions CreateDeletionCookieOptions(HttpRequest request) => new()
    {
        HttpOnly = true,
        Secure = IsBrowserHttps(request),
        SameSite = ShouldUseCrossSiteCookie(request) && IsBrowserHttps(request) ? SameSiteMode.None : SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
        Domain = null, // Allow cookie to be sent to proxied requests
        Expires = DateTimeOffset.UtcNow.AddDays(-1),
    };

    private static bool IsBrowserHttps(HttpRequest request)
    {
        var frontendProto = request.Headers["X-Forwarded-Proto"].ToString();
        if (string.Equals(frontendProto, "https", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(frontendProto, "http", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        frontendProto = request.Headers["X-Frontend-Proto"].ToString();
        if (string.Equals(frontendProto, "https", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(frontendProto, "http", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return request.IsHttps;
    }

    private static bool ShouldUseCrossSiteCookie(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Origin", out var originValues))
        {
            return false;
        }

        var origin = originValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        var requestOrigin = $"{request.Scheme}://{request.Host.Value}";
        return !string.Equals(originUri.GetLeftPart(UriPartial.Authority), requestOrigin, StringComparison.OrdinalIgnoreCase);
    }
}