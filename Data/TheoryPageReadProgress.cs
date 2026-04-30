namespace LearnToCode.Data;

public class TheoryPageReadProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int TheoryContentId { get; set; }

    public TheoryContent TheoryContent { get; set; } = null!;

    public int ContentVersion { get; set; }

    public int PageIndex { get; set; }

    public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;
}
