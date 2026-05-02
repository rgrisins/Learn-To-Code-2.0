namespace LearnToCode.API.Contracts.Representations;

public class RepresentationCreateRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class RepresentationResponse
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

    public bool IsMember { get; set; }

    public bool IsOwner { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

public class RepresentationMemberResponse
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int Rating { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}
