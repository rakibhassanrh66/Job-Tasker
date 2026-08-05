// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Api.Authorization;

/// <summary>
/// Role names for [Authorize(Roles = ...)], which only accepts constant strings.
///
/// A static check ties each one back to the enum, so renaming a role member breaks the
/// build here rather than silently leaving an endpoint guarded by a string that no token
/// will ever carry — a typo that would fail open on every request.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public const string AdminOrTeacher = $"{Admin},{Teacher}";

    static Roles()
    {
        Verify(Admin, UserRole.Admin);
        Verify(Teacher, UserRole.Teacher);
        Verify(Student, UserRole.Student);
    }

    private static void Verify(string name, UserRole role)
    {
        if (!string.Equals(name, role.ToString(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Role constant '{name}' no longer matches UserRole.{role}. " +
                "Authorization attributes would silently stop matching any token.");
        }
    }
}
