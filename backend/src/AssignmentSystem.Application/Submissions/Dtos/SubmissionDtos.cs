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
