namespace LearnToCode.Data;

public class TheoryContent
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public TheoryTopic? Topic { get; set; }

    public string MarkdownObjectName { get; set; } = string.Empty;

    public int PageCount { get; set; } = 1;

    public int Version { get; set; } = 1;

    public ICollection<TheoryPageReadProgress> ReadProgress { get; set; } = [];
}
