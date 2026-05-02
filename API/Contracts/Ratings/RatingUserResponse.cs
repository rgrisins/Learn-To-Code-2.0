namespace LearnToCode.API.Contracts.Ratings;

public class RatingUserResponse
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? Representation { get; set; }

    public int Rating { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class RatingRepresentationResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int MemberCount { get; set; }

    public int TotalRating { get; set; }

    public int AverageRating { get; set; }

    public int TheoryProgressPercent { get; set; }

    public int ExerciseSolved { get; set; }

    public int ExerciseSubmissionCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
