// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Assignments.Dtos;

public record AssignmentDto(
    Guid Id,
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    AssignmentStatus Status,
    Guid ClassCourseId,
    string ClassCourseCode,
    Guid SubjectId,
    string SubjectName,
    Guid CreatedByTeacherId,
    string CreatedByTeacherName,
    bool AllowLateSubmission,
    bool AllowUpdateBeforeDeadline,
    int SubmissionCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class AssignmentListQuery : PagedQuery
{
    public AssignmentStatus? Status { get; set; }

    public Guid? ClassCourseId { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? TeacherId { get; set; }

    public string? Search { get; set; }
}

/// <summary>
/// Filters a student may use on GET /assignments/available.
///
/// Deliberately not <see cref="AssignmentListQuery"/>. A student's list is always
/// Published and always their own enrolled classes, so Status cannot mean anything here.
/// Sharing the wider type would have Swagger advertise a status filter that the endpoint
/// silently discards — the parameter does not exist on this route, so it should not exist
/// on its query type either.
/// </summary>
public class StudentAssignmentListQuery : PagedQuery
{
    public Guid? ClassCourseId { get; set; }

    public Guid? SubjectId { get; set; }

    /// <summary>Filters to one teacher's assignments. Safe to expose: the query stays
    /// scoped to published assignments in the student's own classes.</summary>
    public Guid? TeacherId { get; set; }

    public string? Search { get; set; }
}
