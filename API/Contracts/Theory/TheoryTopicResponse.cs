namespace LearnToCode.API.Contracts.Theory;

public class TheoryTopicResponse
{
    public string Id { get; set; } = string.Empty;

    public string LanguageId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public int PageCount { get; set; }

    public int ProgressPercent { get; set; }

    public bool HasQuiz { get; set; }

    public int QuizQuestionCount { get; set; }

    public int QuizAnsweredCount { get; set; }

    public int QuizCorrectAnswerCount { get; set; }
}
