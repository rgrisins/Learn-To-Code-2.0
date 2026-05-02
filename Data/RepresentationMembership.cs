namespace LearnToCode.Data;

public enum RepresentationMemberRole
{
    Owner,
    Member,
}

public class RepresentationMembership
{
    public int Id { get; set; }

    public int RepresentationId { get; set; }

    public Representation Representation { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public RepresentationMemberRole Role { get; set; } = RepresentationMemberRole.Member;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
