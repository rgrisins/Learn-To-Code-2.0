namespace LearnToCode.Data;

public enum SubmissionStatus
{
    Pending,
    Running,
    Passed,
    Failed,
    Error,
    TimedOut,
}

public class ExerciseSubmission
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string LanguageCode { get; set; } = "python";
    public string Code { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public int TestsPassed { get; set; }
    public int TestsTotal { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TestResultsJson { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAtUtc { get; set; }
}
