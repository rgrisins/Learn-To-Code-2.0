namespace LearnToCode.Data;

public enum ExerciseRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

/// <summary>
/// An exercise authored by a Pedagogs/Administrators user that is waiting for an
/// administrator to review. Approving a request materialises an Exercise + TestCases.
/// Until then the exercise itself does not exist.
/// </summary>
public class ExerciseRequest
{
    public int Id { get; set; }

    public int? AuthorId { get; set; }

    public User? Author { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "python";

    public string LanguageVersion { get; set; } = "3.11";

    public string SolutionLanguageCode { get; set; } = "python";

    public string SolutionCode { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialised list of test cases (input, expectedOutput, isHidden) in order.
    /// </summary>
    public string TestCasesJson { get; set; } = string.Empty;

    public ExerciseRequestStatus Status { get; set; } = ExerciseRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
