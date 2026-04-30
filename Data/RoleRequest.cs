namespace LearnToCode.Data;

public class RoleRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public UserRole RequestedRole { get; set; } = UserRole.Pedagogs;

    public string Reason { get; set; } = string.Empty;

    public RoleRequestStatus Status { get; set; } = RoleRequestStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
