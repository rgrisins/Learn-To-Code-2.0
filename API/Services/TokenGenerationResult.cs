namespace LearnToCode.API.Services;

public sealed class TokenGenerationResult
{
    public string Token { get; init; } = string.Empty;

    public string JwtId { get; init; } = string.Empty;

    public DateTime ExpiresAtUtc { get; init; }
}