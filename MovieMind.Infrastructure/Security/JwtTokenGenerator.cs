using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MovieMind.Application.DTOs.Auth;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public IssuedAccessToken Generate(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer))
            throw new InvalidOperationException("JWT Issuer nu este configurat.");

        if (string.IsNullOrWhiteSpace(_options.Audience))
            throw new InvalidOperationException("JWT Audience nu este configurat.");

        if (_options.AccessTokenMinutes is < 1 or > 1440)
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes trebuie să fie între 1 și 1440.");

        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);

        if (keyBytes.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKey trebuie să aibă cel puțin 32 de bytes.");

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new IssuedAccessToken(token, expiresAt);
    }
}