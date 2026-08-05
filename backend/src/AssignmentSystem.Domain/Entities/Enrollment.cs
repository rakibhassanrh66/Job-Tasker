// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// Places a student in a class. Unique on (StudentId, ClassCourseId) so the same student
/// cannot be enrolled twice. This table is the sole source of truth for which assignments
/// a student may see (business rule 2).
/// </summary>
public class Enrollment
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    public User Student { get; set; } = null!;

    public Guid ClassCourseId { get; set; }

    public ClassCourse ClassCourse { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
