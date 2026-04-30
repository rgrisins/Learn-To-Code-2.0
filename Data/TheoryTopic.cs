namespace LearnToCode.Data;

public class TheoryTopic
{
    public int Id { get; set; }

    public int LanguageId { get; set; }

    public TheoryLanguage? Language { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public int SortOrder { get; set; }

    public TheoryContent? Content { get; set; }

    public TheoryQuiz? Quiz { get; set; }
}
