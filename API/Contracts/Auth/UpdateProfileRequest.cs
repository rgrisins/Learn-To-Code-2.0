namespace LearnToCode.API.Contracts.Auth;

public class UpdateProfileRequest
{
    public string Username { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string? EducationInstitution { get; set; }
}