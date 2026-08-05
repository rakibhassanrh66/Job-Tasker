// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Entities;

/// <summary>
/// A class or course. Students join one via <see cref="Enrollment"/>, and that
/// membership is what scopes which assignments they can see (business rule 2).
/// </summary>
public class ClassCourse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Unique short code, e.g. "CS-101".</summary>
    public string Code { get; set; } = null!;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
