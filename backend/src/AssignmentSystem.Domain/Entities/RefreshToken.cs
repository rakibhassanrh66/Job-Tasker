// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A stored, rotatable refresh token.
///
/// Not part of the specified data model — added because the auth design requires refresh
/// tokens to be "stored/rotated", and rotation without server-side state is cosmetic: a
/// stolen token could not be revoked. Documented in README under Assumptions.
///
/// Only a hash of the token is persisted, for the same reason passwords are hashed —
/// a leaked database dump must not yield usable credentials.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>SHA-256 of the token value. The raw token exists only in the response
    /// to the client and is never written down here.</summary>
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the token is rotated or explicitly revoked.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Points at the token issued in its place, so a rotation chain can be
    /// walked if a revoked token is ever replayed.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow <= ExpiresAt;
}
