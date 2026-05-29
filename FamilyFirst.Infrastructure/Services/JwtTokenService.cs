using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FamilyFirst.Application.Services.Interfaces;
using FamilyFirst.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FamilyFirst.Infrastructure.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, AuthTokenContext tokenContext)
    {
        var issuer = _configuration["Jwt:Issuer"] ?? "familyfirst.app";
        var audience = _configuration["Jwt:Audience"] ?? "familyfirst.users";
        var expiryMinutes = int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var configuredExpiry)
            ? configuredExpiry
            : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(ClaimTypes.Name, user.FullName),
            new("phone", user.PhoneNumber),
            new("role", tokenContext.Role),
            new(ClaimTypes.Role, tokenContext.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        AddOptionalClaim(claims, "familyId", tokenContext.FamilyId);
        AddOptionalClaim(claims, "familyMemberId", tokenContext.FamilyMemberId);
        AddOptionalClaim(claims, "planCode", tokenContext.PlanCode);
        AddOptionalClaim(claims, "childProfileId", tokenContext.ChildProfileId);
        AddOptionalClaim(claims, "teacherProfileId", tokenContext.TeacherProfileId);

        if (tokenContext.AssignedChildIds.Count > 0)
        {
            claims.Add(new Claim("assignedChildIds", string.Join(",", tokenContext.AssignedChildIds)));
        }

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(CreateSigningKey(), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }

    private SymmetricSecurityKey CreateSigningKey()
    {
        var secret = _configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is missing.");
        }

        return new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    private static void AddOptionalClaim(List<Claim> claims, string claimType, Guid? value)
    {
        if (value.HasValue)
        {
            claims.Add(new Claim(claimType, value.Value.ToString()));
        }
    }

    private static void AddOptionalClaim(List<Claim> claims, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimType, value));
        }
    }
}
