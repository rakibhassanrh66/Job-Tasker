// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Infrastructure.Auth;

/// <summary>
/// Wraps ASP.NET Core's <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA256, per-password
/// salt, iteration count encoded in the hash so it can be raised later without invalidating
/// existing hashes).
/// </summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <summary>The framework's default implementation ignores the user argument entirely;
    /// a shared instance avoids allocating one per call.</summary>
    private static readonly User HashingContext = new();

    public string Hash(string password) => _hasher.HashPassword(HashingContext, password);

    public bool Verify(string hash, string password)
    {
        var result = _hasher.VerifyHashedPassword(HashingContext, hash, password);

        // SuccessRehashNeeded also means the password was correct — it only signals that
        // the stored hash used older parameters and could be upgraded.
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
