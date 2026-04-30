namespace LearnToCode.API.Contracts.Auth;

public class CreateRoleRequestRequest
{
    public string RequestedRole { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}

public class RoleRequestResponse
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Username { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string CurrentRole { get; set; } = string.Empty;

    public string RequestedRole { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
