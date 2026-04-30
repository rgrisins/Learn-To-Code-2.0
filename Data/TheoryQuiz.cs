namespace LearnToCode.Data;

public class TheoryQuiz
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public TheoryTopic? Topic { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<TheoryQuizQuestion> Questions { get; set; } = new List<TheoryQuizQuestion>();
}
