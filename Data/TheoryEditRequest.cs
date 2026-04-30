namespace LearnToCode.Data;

public class TheoryTopicRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public TheoryTopicRequestType RequestType { get; set; } = TheoryTopicRequestType.Add;

    public string LanguageCode { get; set; } = string.Empty;

    public string TopicSlug { get; set; } = string.Empty;

    public string? ProposedTitle { get; set; }

    public string? ProposedDescription { get; set; }

    public string? ProposedDifficulty { get; set; }

    public int? ProposedEstimatedMinutes { get; set; }

    public string? ProposedMarkdown { get; set; }

    public TheoryRequestStatus Status { get; set; } = TheoryRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
