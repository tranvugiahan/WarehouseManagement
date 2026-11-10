using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services;

public sealed class JwtTokenService(string issuer, string audience, string key)
{
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

    public LoginResult Create(User user, string sessionId, string refreshToken, DateTime refreshExpiresAt)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var token = new JwtSecurityToken(
            issuer, audience,
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
             new Claim("sid", sessionId),
             new Claim(ClaimTypes.Name, user.Username),
             new Claim(ClaimTypes.Role, user.Role)],
            expires: expiresAt, signingCredentials: _credentials);

        return new LoginResult(new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt, user.Id, user.FullName, user.Role, refreshToken, refreshExpiresAt);
    }
}
