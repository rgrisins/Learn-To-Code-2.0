namespace LearnToCode.Data;

public class TheoryLanguage
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<TheoryTopic> Topics { get; set; } = new List<TheoryTopic>();
}
