namespace LearnToCode.Data;

public enum RepresentationJoinRequestStatus
{
    Pending,
    Approved,
    Rejected,
}

public class RepresentationJoinRequest
{
    public int Id { get; set; }

    public int RepresentationId { get; set; }

    public Representation Representation { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public RepresentationJoinRequestStatus Status { get; set; } = RepresentationJoinRequestStatus.Pending;

    public string? Message { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAtUtc { get; set; }

    public int? ResolvedByUserId { get; set; }

    public User? ResolvedByUser { get; set; }
}
