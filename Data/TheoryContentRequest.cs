namespace LearnToCode.Data;

public class TheoryContentRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public string TopicSlug { get; set; } = string.Empty;

    public TheoryContentRequestType RequestType { get; set; } = TheoryContentRequestType.Add;

    public int? PageIndex { get; set; }

    public string ProposedMarkdown { get; set; } = string.Empty;

    public TheoryRequestStatus Status { get; set; } = TheoryRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
