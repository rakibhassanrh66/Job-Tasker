// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Assignments.Dtos;

/// <summary>
/// An assignment as a student sees it.
///
/// Carries the student's own submission state alongside the assignment so a list screen
/// can show "submitted" or "overdue" without a second request per row — and so the submit
/// button can be disabled from the same payload that rendered it. Deliberately omits the
/// submission count and the authoring teacher's id, which are none of a student's business.
/// </summary>
public record StudentAssignmentDto(
    Guid Id,
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    Guid ClassCourseId,
    string ClassCourseCode,
    Guid SubjectId,
    string SubjectName,
    string TeacherName,
    bool AllowLateSubmission,
    bool AllowUpdateBeforeDeadline,
    bool HasSubmitted,
    Guid? SubmissionId,
    SubmissionStatus? SubmissionStatus,
    int? Marks);
