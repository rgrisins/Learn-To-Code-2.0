namespace LearnToCode.API.Contracts.Theory;

public class TheoryPageResponse
{
    public string LanguageId { get; set; } = string.Empty;

    public string TopicId { get; set; } = string.Empty;

    public string TopicTitle { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public int PageIndex { get; set; }

    public int PageCount { get; set; }

    public bool IsRead { get; set; }

    public int TopicProgressPercent { get; set; }

    public string Markdown { get; set; } = string.Empty;
}
