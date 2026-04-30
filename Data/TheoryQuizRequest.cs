namespace LearnToCode.Data;

public class TheoryQuizRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public TheoryQuizRequestType RequestType { get; set; } = TheoryQuizRequestType.Add;

    public string LanguageCode { get; set; } = string.Empty;

    public string TopicSlug { get; set; } = string.Empty;

    public string ProposedTitle { get; set; } = string.Empty;

    public string ProposedDescription { get; set; } = string.Empty;

    public string ProposedQuestionsJson { get; set; } = string.Empty;

    public TheoryRequestStatus Status { get; set; } = TheoryRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
