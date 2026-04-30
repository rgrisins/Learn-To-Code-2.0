namespace LearnToCode.API.Contracts.Theory;

public record TheoryQuizSummaryDto(
    int Id,
    int TopicId,
    string TopicSlug,
    string TopicTitle,
    string LanguageCode,
    string Title,
    string Description,
    int QuestionCount,
    int CorrectAnswerCount,
    bool IsCompleted
);

public record TheoryQuizDto(
    int Id,
    int TopicId,
    string TopicSlug,
    string TopicTitle,
    string LanguageCode,
    string Title,
    string Description,
    int QuestionCount,
    int CorrectAnswerCount,
    IReadOnlyList<TheoryQuizQuestionDto> Questions
);

public record TheoryQuizQuestionDto(
    int Id,
    int OrderIndex,
    string Prompt,
    string? Explanation,
    bool IsAnswered,
    bool IsAnsweredCorrectly,
    int? SelectedOptionId,
    int? CorrectOptionId,
    IReadOnlyList<TheoryQuizOptionDto> Options
);

public record TheoryQuizOptionDto(
    int Id,
    int OrderIndex,
    string Text
);

public record TheoryQuizManagementDto(
    int Id,
    int TopicId,
    string TopicSlug,
    string TopicTitle,
    string LanguageCode,
    string Title,
    string Description,
    IReadOnlyList<TheoryQuizManagementQuestionDto> Questions
);

public record TheoryQuizManagementQuestionDto(
    int Id,
    int OrderIndex,
    string Prompt,
    string? Explanation,
    int CorrectOptionIndex,
    IReadOnlyList<string> Options
);

public record TheoryQuizAnswerRequest(int QuestionId, int OptionId);

public record TheoryQuizAnswerResultDto(
    int QuestionId,
    int SelectedOptionId,
    int CorrectOptionId,
    bool IsCorrect,
    bool RatingAwarded,
    int RatingDelta,
    string? Explanation
);
