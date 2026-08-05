// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// Password hashing, abstracted so the Application layer never takes a dependency on a
/// specific hashing implementation. Infrastructure supplies ASP.NET Core's
/// PasswordHasher (PBKDF2, salted, iteration count versioned in the hash itself).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>True when <paramref name="password"/> matches <paramref name="hash"/>.</summary>
    bool Verify(string hash, string password);
}
