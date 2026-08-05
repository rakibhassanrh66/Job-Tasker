// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Linq.Expressions;
using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Domain.Entities;

namespace AssignmentSystem.Application.Common;

/// <summary>
/// Entity-to-DTO projections, defined once as expressions so EF translates them into the
/// SELECT rather than materialising entities first.
///
/// Keeping them in one place means every endpoint returns the same shape, and a field
/// added to an entity does not start appearing in responses just because someone forgot
/// there were four separate projections to think about.
/// </summary>
public static class Projections
{
    public static Expression<Func<Assignment, AssignmentDto>> ToAssignmentDto =>
        a => new AssignmentDto(
            a.Id,
            a.Title,
            a.Description,
            a.Deadline,
            a.MaxMarks,
            a.Status,
            a.ClassCourseId,
            a.ClassCourse.Code,
            a.SubjectId,
            a.Subject.Name,
            a.CreatedByTeacherId,
            a.CreatedByTeacher.FullName,
            a.AllowLateSubmission,
            a.AllowUpdateBeforeDeadline,
            a.Submissions.Count,
            a.CreatedAt,
            a.UpdatedAt);

    public static Expression<Func<Submission, SubmissionDto>> ToSubmissionDto =>
        s => new SubmissionDto(
            s.Id,
            s.AssignmentId,
            s.Assignment.Title,
            s.StudentId,
            s.Student.FullName,
            s.Student.Email,
            s.AnswerText,
            s.AttachmentUrl,
            s.Status,
            s.SubmittedAt,
            s.UpdatedAt,
            s.Marks,
            s.Assignment.MaxMarks,
            s.Feedback,
            s.GradedByTeacherId,
            s.GradedByTeacher != null ? s.GradedByTeacher.FullName : null,
            s.GradedAt);
}
