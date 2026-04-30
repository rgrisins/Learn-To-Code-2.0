namespace LearnToCode.API.Contracts.Theory;

public class TheoryLanguageResponse
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public int TopicCount { get; set; }

    public int ProgressPercent { get; set; }
}
