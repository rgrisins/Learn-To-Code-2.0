namespace LearnToCode.API.Contracts.Ratings;

public class RatingUserResponse
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? EducationInstitution { get; set; }

    public int Rating { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
