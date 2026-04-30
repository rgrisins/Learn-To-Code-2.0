namespace LearnToCode.Data;

public class User
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? NormalizedUsername { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? EducationInstitution { get; set; }

    public int Rating { get; set; } = 1000;

    public UserRole Role { get; set; } = UserRole.Audzeknis;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}