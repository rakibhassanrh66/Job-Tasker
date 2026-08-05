// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// The ownership half of authorization, and the half a role check cannot cover.
///
/// [Authorize(Roles = "Teacher")] establishes that the caller is *a* teacher. It says
/// nothing about whether this is *their* assignment. Each method here answers that second
/// question and throws <see cref="Domain.Exceptions.ForbiddenResourceException"/> — mapped
/// to 403 — when the answer is no.
///
/// Every method takes the acting user's id explicitly rather than reading it internally,
/// so a caller cannot accidentally pass a client-supplied id: at the call site it is
/// always visibly sourced from the token.
/// </summary>
public interface IResourceAuthorizer
{
    /// <summary>Business rule 4: a teacher may only act on assignments they created.</summary>
    void EnsureTeacherOwnsAssignment(Guid teacherId, Assignment assignment);

    /// <summary>Business rule 3: a teacher may only create assignments for a
    /// (Subject, ClassCourse) pair allocated to them in TeacherAssignments.</summary>
    Task EnsureTeacherTeachesSubjectInClassAsync(
        Guid teacherId, Guid subjectId, Guid classCourseId, CancellationToken cancellationToken = default);

    /// <summary>Business rule 8: a student may only read or update their own submission.</summary>
    void EnsureStudentOwnsSubmission(Guid studentId, Submission submission);

    /// <summary>Business rule 2: a student may only reach assignments belonging to a class
    /// they are enrolled in.</summary>
    Task EnsureStudentEnrolledInClassAsync(
        Guid studentId, Guid classCourseId, CancellationToken cancellationToken = default);
}
