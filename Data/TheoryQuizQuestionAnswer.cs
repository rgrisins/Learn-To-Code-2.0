namespace LearnToCode.Data;

/// <summary>
/// Records the first answer a user gives to a quiz question.
/// Used to make rating awards idempotent: a user gets +1 rating point only for
/// a correct first answer, and answered questions cannot be retried.
/// </summary>
public class TheoryQuizQuestionAnswer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    public TheoryQuizQuestion? Question { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public int? SelectedOptionId { get; set; }

    public TheoryQuizOption? SelectedOption { get; set; }

    public bool IsCorrect { get; set; }

    public DateTime AnsweredAtUtc { get; set; } = DateTime.UtcNow;
}
