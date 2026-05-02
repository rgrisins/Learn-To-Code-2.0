using LearnToCode.API.Contracts.Auth;
using LearnToCode.Data;

namespace LearnToCode.API.Services;

public static class RoleRequestMapper
{
    public static RoleRequestResponse ToResponse(RoleRequest request)
    {
        var user = request.User;

        return new RoleRequestResponse
        {
            Id = request.Id,
            UserId = request.UserId,
            Username = user?.Username,
            FullName = user?.FullName ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            CurrentRole = user?.Role.ToString() ?? string.Empty,
            RequestedRole = request.RequestedRole.ToString(),
            Reason = request.Reason,
            Status = request.Status.ToString(),
            CreatedAtUtc = request.CreatedAtUtc,
        };
    }
}
