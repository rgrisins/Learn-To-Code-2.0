namespace LearnToCode.API.Contracts.Auth;

public class ProfileStatsResponse
{
    public IReadOnlyList<ProfileTheoryLanguageProgressResponse> TheoryLanguages { get; set; } = [];

    public int ExerciseTotal { get; set; }

    public int ExerciseAttempted { get; set; }

    public int ExerciseSolved { get; set; }

    public int ExerciseSubmissionCount { get; set; }

    public int ExercisePassedSubmissions { get; set; }

    public int ExerciseFailedSubmissions { get; set; }

    public int ExerciseCompletionPercent { get; set; }

    public int ExerciseSuccessPercent { get; set; }

    public string? MostUsedExerciseLanguage { get; set; }

    public int MostUsedExerciseLanguageSubmissionCount { get; set; }
}

public class ProfileTheoryLanguageProgressResponse
{
    public string LanguageId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int TopicCount { get; set; }

    public int ProgressPercent { get; set; }
}
