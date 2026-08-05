// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Submissions;

public interface ISubmissionService
{
    /// <summary>Unfiltered listing for administrative oversight.</summary>
    Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken cancellationToken = default);

    Task<SubmissionDto> GradeAsync(
        Guid id, GradeSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<SubmissionDto> ChangeStatusAsync(
        Guid id, ChangeSubmissionStatusRequest request, CancellationToken cancellationToken = default);
}

public class SubmissionService : ISubmissionService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IClock _clock;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IResourceAuthorizer authorizer,
        IClock clock,
        ILogger<SubmissionService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _authorizer = authorizer;
        _clock = clock;
        _logger = logger;
    }

    public async Task<PagedResult<SubmissionDto>> ListAllAsync(
        SubmissionListQuery query, CancellationToken cancellationToken = default)
    {
        var submissions = _db.Submissions.AsNoTracking();

        if (query.AssignmentId is not null)
        {
            submissions = submissions.Where(s => s.AssignmentId == query.AssignmentId);
        }

        if (query.StudentId is not null)
        {
            submissions = submissions.Where(s => s.StudentId == query.StudentId);
        }

        if (query.Status is not null)
        {
            submissions = submissions.Where(s => s.Status == query.Status);
        }

        return await submissions
            .OrderByDescending(s => s.SubmittedAt)
            .Select(Projections.ToSubmissionDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    /// <summary>
    /// Records marks and feedback. Enforces, in order: that the caller owns the parent
    /// assignment (rule 4), that the submission is in a gradeable state (rule 10), and
    /// that the marks fall within [0, MaxMarks] (rule 9).
    /// </summary>
    public async Task<SubmissionDto> GradeAsync(
        Guid id, GradeSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();
        var submission = await LoadWithAssignmentAsync(id, cancellationToken);

        // Rule 4 first: a teacher who does not own the assignment learns nothing further
        // about it, not even whether the marks they tried to enter were in range.
        _authorizer.EnsureTeacherOwnsAssignment(teacherId, submission.Assignment);

        SubmissionStatusPolicy.EnsureCanGrade(submission.Status);

        // Rule 9. The upper bound lives here rather than in a database CHECK because
        // MaxMarks is on the parent row and PostgreSQL cannot reach across tables in a
        // check constraint. The lower bound is additionally enforced by the database.
        if (request.Marks < 0 || request.Marks > submission.Assignment.MaxMarks)
        {
            throw new MarksExceedMaxException(request.Marks, submission.Assignment.MaxMarks);
        }

        submission.Marks = request.Marks;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback)
            ? null
            : request.Feedback.Trim();
        submission.Status = SubmissionStatus.Graded;
        submission.GradedByTeacherId = teacherId;
        submission.GradedAt = _clock.UtcNow;
        submission.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} graded submission {SubmissionId} as {Marks}/{MaxMarks}.",
            teacherId, id, request.Marks, submission.Assignment.MaxMarks);

        return await ProjectAsync(id, cancellationToken);
    }

    /// <summary>Moves a submission through the lifecycle explicitly. Business rule 10.</summary>
    public async Task<SubmissionDto> ChangeStatusAsync(
        Guid id, ChangeSubmissionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();
        var submission = await LoadWithAssignmentAsync(id, cancellationToken);

        _authorizer.EnsureTeacherOwnsAssignment(teacherId, submission.Assignment);

        SubmissionStatusPolicy.EnsureCanTransition(submission.Status, request.Status);

        submission.Status = request.Status;
        submission.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} moved submission {SubmissionId} to {Status}.",
            teacherId, id, request.Status);

        return await ProjectAsync(id, cancellationToken);
    }

    // ---------------------------------------------------------------------------------

    private async Task<Submission> LoadWithAssignmentAsync(
        Guid id, CancellationToken cancellationToken) =>
        await _db.Submissions
            .Include(s => s.Assignment)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Submission", id);

    private async Task<SubmissionDto> ProjectAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Submissions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(Projections.ToSubmissionDto)
            .SingleAsync(cancellationToken);
}
