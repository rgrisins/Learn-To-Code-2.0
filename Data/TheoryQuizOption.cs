namespace LearnToCode.Data;

public class TheoryQuizOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public TheoryQuizQuestion? Question { get; set; }

    public int OrderIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}
