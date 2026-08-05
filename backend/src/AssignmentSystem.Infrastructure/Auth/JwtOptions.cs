// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Text;

namespace AssignmentSystem.Infrastructure.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key, supplied from the environment. Never has a default:
    /// a fallback key would silently become the production key the day someone forgets to
    /// set it, and every token in the system would be forgeable by anyone reading this
    /// repository.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;

    /// <summary>HMAC-SHA256 requires a key at least as long as its output. A shorter key is
    /// rejected loudly at startup rather than producing weak signatures at runtime.</summary>
    public const int MinimumKeyBytes = 32;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException(
                "Jwt__Key is not configured. Copy .env.example to .env and set a generated key.");
        }

        if (Encoding.UTF8.GetByteCount(Key) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt__Key must be at least {MinimumKeyBytes} bytes for HMAC-SHA256; " +
                $"the configured value is {Encoding.UTF8.GetByteCount(Key)} bytes.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt__Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt__Audience is not configured.");
        }

        if (AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt__AccessTokenMinutes must be greater than zero.");
        }

        if (RefreshTokenDays <= 0)
        {
            throw new InvalidOperationException("Jwt__RefreshTokenDays must be greater than zero.");
        }
    }
}
