// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Auth.Dtos;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

/// <param name="AccessToken">Short-lived bearer token.</param>
/// <param name="RefreshToken">Raw refresh token. Returned once — only its hash is stored,
/// so it cannot be recovered from the database afterwards.</param>
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfile User);

/// <summary>The caller's own profile. Deliberately excludes PasswordHash — DTOs are
/// projected explicitly rather than returning entities, so a field cannot start leaking
/// because someone added it to the entity.</summary>
public record UserProfile(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive)
{
    public static UserProfile From(User user) =>
        new(user.Id, user.FullName, user.Email, user.Role, user.IsActive);
}
