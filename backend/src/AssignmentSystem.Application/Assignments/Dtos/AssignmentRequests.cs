// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Application.Assignments.Dtos;

public record CreateAssignmentRequest(
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    Guid ClassCourseId,
    Guid SubjectId,
    bool AllowLateSubmission,
    bool AllowUpdateBeforeDeadline);

/// <summary>
/// Class and subject are deliberately absent.
///
/// Moving an assignment to a different class or subject after it exists would change who
/// it belongs to — potentially stranding submissions from students who are not in the new
/// class, and shifting it into a subject the teacher may not even teach. Creating a new
/// assignment is the honest way to express that intent.
/// </summary>
public record UpdateAssignmentRequest(
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    bool AllowLateSubmission,
    bool AllowUpdateBeforeDeadline);
