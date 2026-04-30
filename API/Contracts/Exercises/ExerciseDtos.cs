namespace LearnToCode.API.Contracts.Exercises;

public record ExerciseListItemDto(
    int Id,
    string Title,
    string Description,
    string Difficulty,
    string LanguageCode,
    string LanguageVersion,
    int TestCaseCount,
    int SubmissionCount,
    int AttemptedUserCount,
    int SolvedAttemptPercent,
    bool IsSolved
);

public record ExerciseDetailDto(
    int Id,
    string Title,
    string Description,
    string Difficulty,
    string LanguageCode,
    string LanguageVersion,
    IEnumerable<ExerciseTestCaseDto> VisibleTestCases,
    bool IsSolved
);

public record ExerciseTestCaseDto(
    int Id,
    string Input,
    string ExpectedOutput,
    int OrderIndex
);

public record SubmitCodeRequest(
    string Code,
    string LanguageCode
);

public record SubmissionResultDto(
    int Id,
    string Status,
    int TestsPassed,
    int TestsTotal,
    string? ErrorMessage,
    IEnumerable<TestCaseResultDto> TestResults,
    int RatingDelta
);

public record SubmissionProgressDto(
    string Type,
    int SubmissionId,
    int Current,
    int Total,
    int TestsPassed,
    string Status,
    string? ErrorMessage,
    TestCaseResultDto? TestResult,
    SubmissionResultDto? Result
);

public record ExerciseSubmissionDto(
    int Id,
    int ExerciseId,
    string ExerciseTitle,
    string Difficulty,
    string LanguageCode,
    string Status,
    int TestsPassed,
    int TestsTotal,
    string? ErrorMessage,
    IEnumerable<TestCaseResultDto> TestResults,
    DateTime SubmittedAtUtc,
    DateTime? ExecutedAtUtc
);

public record TestCaseResultDto(
    int OrderIndex,
    bool IsHidden,
    bool Passed,
    string? ActualOutput,
    string? ExpectedOutput,
    string? ErrorMessage
);

public record CreateExerciseRequest(
    string Title,
    string Description,
    string Difficulty,
    string LanguageCode,
    string SolutionLanguageCode,
    string SolutionCode,
    IEnumerable<CreateTestCaseRequest> TestCases
);

public record CreateTestCaseRequest(
    string Input,
    string ExpectedOutput,
    bool IsHidden,
    int OrderIndex
);

public record ExercisePendingDto(
    int Id,
    string Title,
    string Description,
    string Difficulty,
    string LanguageCode,
    string LanguageVersion,
    int? AuthorId,
    string AuthorName,
    string AuthorEmail,
    DateTime CreatedAtUtc,
    string Status,
    IReadOnlyList<ExercisePendingTestCaseDto> TestCases
);

public record ExercisePendingTestCaseDto(
    int OrderIndex,
    bool IsHidden,
    string Input,
    string ExpectedOutput
);
