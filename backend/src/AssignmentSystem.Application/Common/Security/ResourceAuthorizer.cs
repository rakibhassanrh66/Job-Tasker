// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Common.Security;

/// <inheritdoc cref="IResourceAuthorizer"/>
public class ResourceAuthorizer : IResourceAuthorizer
{
    private readonly IAppDbContext _db;

    public ResourceAuthorizer(IAppDbContext db) => _db = db;

    public void EnsureTeacherOwnsAssignment(Guid teacherId, Assignment assignment)
    {
        if (assignment.CreatedByTeacherId != teacherId)
        {
            throw new ForbiddenResourceException(
                "You may only act on assignments you created.");
        }
    }

    public async Task EnsureTeacherTeachesSubjectInClassAsync(
        Guid teacherId,
        Guid subjectId,
        Guid classCourseId,
        CancellationToken cancellationToken = default)
    {
        var teaches = await _db.TeacherAssignments
            .AsNoTracking()
            .AnyAsync(
                t => t.TeacherId == teacherId
                     && t.SubjectId == subjectId
                     && t.ClassCourseId == classCourseId,
                cancellationToken);

        if (!teaches)
        {
            throw new ForbiddenResourceException(
                "You are not assigned to teach this subject in this class.");
        }
    }

    public void EnsureStudentOwnsSubmission(Guid studentId, Submission submission)
    {
        if (submission.StudentId != studentId)
        {
            throw new ForbiddenResourceException(
                "You may only access your own submissions.");
        }
    }

    public async Task EnsureStudentEnrolledInClassAsync(
        Guid studentId,
        Guid classCourseId,
        CancellationToken cancellationToken = default)
    {
        var enrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.StudentId == studentId && e.ClassCourseId == classCourseId,
                cancellationToken);

        if (!enrolled)
        {
            throw new ForbiddenResourceException(
                "You are not enrolled in the class this assignment belongs to.");
        }
    }
}
