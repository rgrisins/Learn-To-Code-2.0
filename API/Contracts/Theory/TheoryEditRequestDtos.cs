using System.ComponentModel.DataAnnotations;

namespace LearnToCode.API.Contracts.Theory;

public class CreateTheoryTopicRequestRequest
{
    [Required]
    public string RequestType { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string LanguageCode { get; set; } = string.Empty;

    [MaxLength(96)]
    public string? TopicSlug { get; set; }

    [MaxLength(160)]
    public string? ProposedTitle { get; set; }

    [MaxLength(900)]
    public string? ProposedDescription { get; set; }

    [MaxLength(40)]
    public string? ProposedDifficulty { get; set; }

    [Range(1, 600)]
    public int? ProposedEstimatedMinutes { get; set; }

    [MaxLength(200000)]
    public string? ProposedMarkdown { get; set; }
}

public class CreateTheoryContentRequestRequest
{
    [MaxLength(32)]
    public string RequestType { get; set; } = "Add";

    [Required, MaxLength(64)]
    public string LanguageCode { get; set; } = string.Empty;

    [Required, MaxLength(96)]
    public string TopicSlug { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int? PageIndex { get; set; }

    [Required, MinLength(1), MaxLength(200000)]
    public string ProposedMarkdown { get; set; } = string.Empty;
}

public class CreateTheoryQuizRequestRequest
{
    [MaxLength(32)]
    public string RequestType { get; set; } = "Add";

    [Required, MaxLength(64)]
    public string LanguageCode { get; set; } = string.Empty;

    [Required, MaxLength(96)]
    public string TopicSlug { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(160)]
    public string ProposedTitle { get; set; } = string.Empty;

    [MaxLength(900)]
    public string ProposedDescription { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public List<TheoryQuizQuestionRequestDto> Questions { get; set; } = [];
}

public class TheoryQuizQuestionRequestDto
{
    [Required, MinLength(1), MaxLength(1000)]
    public string Prompt { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Explanation { get; set; }

    [Required, MinLength(4), MaxLength(4)]
    public List<string> Options { get; set; } = [];

    [Range(0, 3)]
    public int CorrectOptionIndex { get; set; }
}

public class TheoryTopicRequestResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string TopicSlug { get; set; } = string.Empty;
    public string? ProposedTitle { get; set; }
    public string? ProposedDescription { get; set; }
    public string? ProposedDifficulty { get; set; }
    public int? ProposedEstimatedMinutes { get; set; }
    public string? ProposedMarkdown { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class TheoryContentRequestResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string TopicSlug { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public int? PageIndex { get; set; }
    public string ProposedMarkdown { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class TheoryQuizRequestResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string TopicSlug { get; set; } = string.Empty;
    public string ProposedTitle { get; set; } = string.Empty;
    public string ProposedDescription { get; set; } = string.Empty;
    public IReadOnlyList<TheoryQuizQuestionRequestDto> Questions { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
