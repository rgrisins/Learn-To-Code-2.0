namespace LearnToCode.Data;

public class Representation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsPublic { get; set; } = true;

    public int? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<RepresentationMembership> Memberships { get; set; } = [];

    public ICollection<RepresentationJoinRequest> JoinRequests { get; set; } = [];
}
