// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentSystem.Infrastructure.Auth;

public class JwtTokenService : ITokenService
{
    /// <summary>Claim type carrying the role. Named plainly rather than using the long
    /// WS-Federation URI; the token validation parameters are configured to read roles
    /// from here, which keeps [Authorize(Roles = "...")] working.</summary>
    public const string RoleClaimType = "role";

    public const string UserIdClaimType = "userId";

    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    public AccessToken CreateAccessToken(User user)
    {
        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(RoleClaimType, user.Role.ToString()),
            new(UserIdClaimType, user.Id.ToString()),

            // Unique per token, so two tokens issued in the same second are distinguishable.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public (string RawToken, string TokenHash) CreateRefreshToken()
    {
        // 256 bits from a cryptographic RNG. Guid.NewGuid() would be the convenient choice
        // and the wrong one: it is not generated for unpredictability.
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Base64UrlEncoder.Encode(bytes);

        return (raw, HashRefreshToken(raw));
    }

    /// <summary>
    /// Plain SHA-256, not a password hash. Deliberate: the token is 256 bits of random
    /// data, so there is no dictionary to attack and nothing for a slow KDF to defend
    /// against — while refresh happens on a hot path where a deliberately slow hash would
    /// just be latency. The reason to hash at all is that a leaked database dump must not
    /// contain usable tokens.
    /// </summary>
    public string HashRefreshToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(hash);
    }
}
