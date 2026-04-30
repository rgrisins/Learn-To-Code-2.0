namespace LearnToCode.API.Contracts.Auth;

public class AuthResponse
{
    public UserProfileResponse User { get; set; } = new();

    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string TokenType { get; set; } = "Bearer";
}
