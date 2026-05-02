using LearnToCode.Data;

namespace LearnToCode.API.Contracts.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? Representation { get; set; }

    public string Role { get; set; } = UserRole.Audzeknis.ToString();

    public string? RoleRequestReason { get; set; }
}
