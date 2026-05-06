namespace LearnToCode.Data;

/// <summary>
/// Glabā lietotāja pirmo atbildi uz teorijas testa jautājumu.
/// Tas nodrošina, ka reitings tiek piešķirts tikai vienu reizi.
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
