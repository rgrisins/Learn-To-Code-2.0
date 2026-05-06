namespace LearnToCode.Data;

public enum ExerciseRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

public enum ExerciseRequestType
{
    Create,
    EditDescription,
}

/// <summary>
/// Pedagoga vai administratora sagatavots uzdevuma pieprasījums,
/// kas kļūst par īstu uzdevumu tikai pēc administratora apstiprinājuma.
/// </summary>
public class ExerciseRequest
{
    public int Id { get; set; }

    public ExerciseRequestType RequestType { get; set; } = ExerciseRequestType.Create;

    public int? ExerciseId { get; set; }

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
    /// JSON formātā saglabāts testu saraksts ar ievadi, sagaidāmo izvadi un slēpšanas pazīmi.
    /// </summary>
    public string TestCasesJson { get; set; } = string.Empty;

    public ExerciseRequestStatus Status { get; set; } = ExerciseRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
