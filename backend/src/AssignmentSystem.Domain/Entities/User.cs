// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A single table holds all three roles, distinguished by <see cref="Role"/>. The roles
/// share every field and differ only in what they may do, so separate tables would buy
/// nothing and would complicate the foreign keys from Assignment and Submission.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    /// <summary>Unique across all users; the login identifier.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Hashed via ASP.NET Core's PasswordHasher. Plaintext is never stored.</summary>
    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; }

    /// <summary>Deactivated users are rejected at login rather than deleted, so their
    /// assignments and graded submissions keep their referential integrity.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation — Student
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    // Navigation — Teacher
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();

    public ICollection<Assignment> CreatedAssignments { get; set; } = new List<Assignment>();

    // Navigation — auth
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
