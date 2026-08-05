// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Submissions.Dtos;

/// <summary>Carries MaxMarks alongside Marks so a client can render "18 / 20" without a
/// second request for the parent assignment.</summary>
public record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string AnswerText,
    string? AttachmentUrl,
    SubmissionStatus Status,
    DateTime SubmittedAt,
    DateTime UpdatedAt,
    int? Marks,
    int MaxMarks,
    string? Feedback,
    Guid? GradedByTeacherId,
    string? GradedByTeacherName,
    DateTime? GradedAt);

public class SubmissionListQuery : PagedQuery
{
    public Guid? AssignmentId { get; set; }

    public Guid? StudentId { get; set; }

    public SubmissionStatus? Status { get; set; }
}

/// <summary>
/// Filters a student may use on GET /submissions/mine.
///
/// StudentId is absent by design: the route is already pinned to the caller, so accepting
/// the parameter would let the API appear to filter by another student and then return
/// the caller's own work regardless — an answer that looks like it honoured the request
/// and did not.
/// </summary>
public class StudentSubmissionListQuery : PagedQuery
{
    public Guid? AssignmentId { get; set; }

    public SubmissionStatus? Status { get; set; }
}

/// <summary>
/// Filters a teacher may use on GET /assignments/{id}/submissions. AssignmentId is absent
/// because the route supplies it; a second one in the query string could only disagree.
/// </summary>
public class AssignmentSubmissionListQuery : PagedQuery
{
    public Guid? StudentId { get; set; }

    public SubmissionStatus? Status { get; set; }
}
