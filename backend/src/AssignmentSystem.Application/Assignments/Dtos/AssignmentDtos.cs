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
