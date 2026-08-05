// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Enums;

/// <summary>
/// The three roles in the system. Values start at 1 deliberately: a default(UserRole)
/// of 0 is not a valid role, so an unassigned field can never silently read as Admin.
/// </summary>
public enum UserRole
{
    Admin = 1,
    Teacher = 2,
    Student = 3
}
