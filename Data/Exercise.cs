namespace LearnToCode.Data;

public class Exercise
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = "python";
    public string LanguageVersion { get; set; } = "3.11";
    public string Difficulty { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int? AuthorId { get; set; }
    public User? Author { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ExerciseTestCase> TestCases { get; set; } = [];
    public ICollection<ExerciseSubmission> Submissions { get; set; } = [];
}
