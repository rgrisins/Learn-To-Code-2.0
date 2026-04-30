namespace LearnToCode.API.Contracts.Theory;

public class TheoryProgressUpdateResponse
{
    public string LanguageId { get; set; } = string.Empty;

    public string TopicId { get; set; } = string.Empty;

    public int PageIndex { get; set; }

    public bool IsRead { get; set; }

    public int LanguageProgressPercent { get; set; }

    public int TopicProgressPercent { get; set; }
}
