namespace LearnToCode.Data;

public class TheoryQuizQuestion
{
    public int Id { get; set; }

    public int QuizId { get; set; }

    public TheoryQuiz? Quiz { get; set; }

    public int OrderIndex { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public ICollection<TheoryQuizOption> Options { get; set; } = new List<TheoryQuizOption>();
}
