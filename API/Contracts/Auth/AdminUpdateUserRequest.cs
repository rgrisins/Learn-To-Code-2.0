namespace LearnToCode.API.Contracts.Auth;

public class AdminUpdateUserRequest
{
    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string Role { get; set; } = string.Empty;

    public int Rating { get; set; }
}
