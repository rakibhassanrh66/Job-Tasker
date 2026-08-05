// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;

namespace AssignmentSystem.Application.Common.Interfaces;

public interface ITokenService
{
    /// <summary>Issues a short-lived access token carrying sub, email and role.</summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>Generates a cryptographically random refresh token. The raw value is
    /// returned to the caller once; only its hash is persisted.</summary>
    (string RawToken, string TokenHash) CreateRefreshToken();

    /// <summary>Hashes a raw refresh token for lookup against stored hashes.</summary>
    string HashRefreshToken(string rawToken);

    TimeSpan RefreshTokenLifetime { get; }
}

/// <param name="Value">The encoded JWT.</param>
/// <param name="ExpiresAtUtc">Absolute expiry, so the client need not decode the token.</param>
public record AccessToken(string Value, DateTime ExpiresAtUtc);
