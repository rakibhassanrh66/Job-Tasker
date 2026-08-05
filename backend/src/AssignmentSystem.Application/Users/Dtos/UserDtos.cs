// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    UserRole Role);

/// <summary>Password is not updatable here on purpose: changing someone else's password is
/// a different operation with different risks, and folding it into a general edit makes it
/// easy to do by accident.</summary>
public record UpdateUserRequest(
    string FullName,
    bool IsActive);

public class UserListQuery : PagedQuery
{
    /// <summary>Optional role filter.</summary>
    public UserRole? Role { get; set; }

    /// <summary>Case-insensitive partial match on name or email.</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}
