// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments;
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

    /// <summary>Creates the calling student's submission for an assignment.</summary>
    Task<SubmissionDto> SubmitAsync(
        Guid assignmentId, CreateSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Replaces the content of the calling student's own submission.</summary>
    Task<SubmissionDto> UpdateAsync(
        Guid id, UpdateSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>The calling student's own submissions, with marks and feedback.</summary>
    Task<PagedResult<SubmissionDto>> ListMineAsync(
        StudentSubmissionListQuery query, CancellationToken cancellationToken = default);

    /// <summary>One of the calling student's own submissions.</summary>
    Task<SubmissionDto> GetMineAsync(Guid id, CancellationToken cancellationToken = default);
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
    // Student — business rules 5, 6, 7 and 8
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Enforces, in order: the assignment is visible at all (rule 1), the student belongs
    /// to its class (rule 2), they have not already submitted (rule 6), and the deadline
    /// permits it (rule 5).
    /// </summary>
    public async Task<SubmissionDto> SubmitAsync(
        Guid assignmentId, CreateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        var assignment = await _db.Assignments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException("Assignment", assignmentId);

        // A draft is answered as not existing, so submitting to one cannot confirm that
        // an unpublished assignment is being prepared.
        if (!AssignmentStatusPolicy.IsVisibleToStudents(assignment.Status))
        {
            throw new NotFoundException("Assignment", assignmentId);
        }

        await _authorizer.EnsureStudentEnrolledInClassAsync(
            studentId, assignment.ClassCourseId, cancellationToken);

        // Rule 6. The unique index on (AssignmentId, StudentId) is the real guarantee —
        // this check exists so the caller gets a 409 that explains itself rather than a
        // database error, but the index is what closes the concurrent-request window.
        var alreadySubmitted = await _db.Submissions.AnyAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId, cancellationToken);

        if (alreadySubmitted)
        {
            throw new DuplicateSubmissionException();
        }

        // Rule 5. Past the deadline the submission is either refused, or accepted and
        // permanently marked Late — never accepted and quietly indistinguishable from
        // work that arrived on time.
        var now = _clock.UtcNow;
        var isLate = assignment.IsPastDeadline(now);

        if (isLate && !assignment.AllowLateSubmission)
        {
            throw SubmissionClosedException.DeadlinePassed();
        }

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            StudentId = studentId,
            AnswerText = request.AnswerText.Trim(),
            AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl)
                ? null
                : request.AttachmentUrl.Trim(),
            Status = isLate ? SubmissionStatus.Late : SubmissionStatus.Submitted,
            SubmittedAt = now,
            UpdatedAt = now
        };

        _db.Submissions.Add(submission);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two concurrent submissions can both pass the check above; the unique index
            // rejects the loser. Translated here so the caller sees the same 409 either
            // way rather than a 500 that depends on timing.
            //
            // The check runs in the catch body rather than an exception filter because
            // filters cannot await. It is re-thrown untouched if the conflict was
            // something else, so a genuine database fault is not disguised as a duplicate.
            if (await ExistsAsync(assignmentId, studentId, cancellationToken))
            {
                throw new DuplicateSubmissionException();
            }

            throw;
        }

        _logger.LogInformation(
            "Student {StudentId} submitted to assignment {AssignmentId} with status {Status}.",
            studentId, assignmentId, submission.Status);

        return await ProjectAsync(submission.Id, cancellationToken);
    }

    /// <summary>
    /// Replaces submission content. Enforces ownership (rule 8) and the update window
    /// (rule 7) — the assignment must permit updates, the deadline must not have passed,
    /// and no teacher may have reviewed the work yet.
    /// </summary>
    public async Task<SubmissionDto> UpdateAsync(
        Guid id, UpdateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();
        var submission = await LoadWithAssignmentAsync(id, cancellationToken);

        // Rule 8, before anything else — a student must not learn whether another
        // student's submission is still editable.
        _authorizer.EnsureStudentOwnsSubmission(studentId, submission);

        // Grading closes the window early, whatever the deadline says. Marks and feedback
        // must always describe the content that was graded — letting a student replace an
        // answer afterwards would leave a teacher's 92/100 attached to work nobody read.
        // Checked before the deadline so the caller gets the specific reason.
        if (submission.Status is not (SubmissionStatus.Submitted or SubmissionStatus.Late))
        {
            throw SubmissionClosedException.AlreadyGraded();
        }

        // Rule 7, both halves: the assignment must permit updates at all, and the deadline
        // must not have passed. A late submission is past the deadline by definition, so
        // it can never be edited afterwards.
        if (!submission.Assignment.AllowUpdateBeforeDeadline)
        {
            throw SubmissionClosedException.UpdatesNotAllowed();
        }

        if (submission.Assignment.IsPastDeadline(_clock.UtcNow))
        {
            throw SubmissionClosedException.DeadlinePassed();
        }

        submission.AnswerText = request.AnswerText.Trim();
        submission.AttachmentUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl)
            ? null
            : request.AttachmentUrl.Trim();
        submission.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(id, cancellationToken);
    }

    public async Task<PagedResult<SubmissionDto>> ListMineAsync(
        StudentSubmissionListQuery query, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        var mine = _db.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId);

        if (query.AssignmentId is not null)
        {
            mine = mine.Where(s => s.AssignmentId == query.AssignmentId);
        }

        if (query.Status is not null)
        {
            mine = mine.Where(s => s.Status == query.Status);
        }

        return await mine
            .OrderByDescending(s => s.SubmittedAt)
            .Select(Projections.ToSubmissionDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<SubmissionDto> GetMineAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();
        var submission = await LoadWithAssignmentAsync(id, cancellationToken);

        _authorizer.EnsureStudentOwnsSubmission(studentId, submission);

        return await ProjectAsync(id, cancellationToken);
    }

    // ---------------------------------------------------------------------------------

    private Task<bool> ExistsAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken) =>
        _db.Submissions.AnyAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId, cancellationToken);

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
