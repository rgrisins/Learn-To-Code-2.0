using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LearnToCode.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace LearnToCode.API.Services;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenGenerationResult CreateToken(User user, string sessionId)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        // Tokenā saglabā minimālos datus tiesību un Redis sesijas pārbaudei.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username ?? user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("sid", sessionId),
        };

        var secret = GetRequiredSetting("Jwt:Secret");
        var issuer = GetRequiredSetting("Jwt:Issuer");
        var audience = GetRequiredSetting("Jwt:Audience");
        var expiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes")
            ?? throw new InvalidOperationException("Jwt:ExpiryMinutes is not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new TokenGenerationResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expires,
            JwtId = jwtId
        };
    }

    private string GetRequiredSetting(string key)
        => _configuration[key] ?? throw new InvalidOperationException($"{key} is not configured.");
}
