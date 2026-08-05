// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A subject taught within a specific <see cref="ClassCourse"/>. A subject belongs to
/// exactly one class, so "Mathematics" in two different classes is two rows.
/// </summary>
public class Subject
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public Guid ClassCourseId { get; set; }

    public ClassCourse ClassCourse { get; set; } = null!;

    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
