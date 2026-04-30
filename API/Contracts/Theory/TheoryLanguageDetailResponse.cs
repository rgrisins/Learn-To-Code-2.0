namespace LearnToCode.API.Contracts.Theory;

public class TheoryLanguageDetailResponse
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public int ProgressPercent { get; set; }

    public IReadOnlyList<TheoryTopicResponse> Topics { get; set; } = [];
}
