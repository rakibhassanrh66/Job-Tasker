// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Records that a teacher teaches a given subject within a given class. Set by an Admin.
///
/// This is what business rule 3 checks: a teacher may only create an assignment for a
/// (Subject, ClassCourse) pair that appears here for them. Named for what it is — a
/// teaching allocation — not to be confused with <see cref="Assignment"/>, the homework.
/// </summary>
public class TeacherAssignment
{
    public Guid Id { get; set; }

    public Guid TeacherId { get; set; }

    public User Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;

    public Guid ClassCourseId { get; set; }

    public ClassCourse ClassCourse { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
