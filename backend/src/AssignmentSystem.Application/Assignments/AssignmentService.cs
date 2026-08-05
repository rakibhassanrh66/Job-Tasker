// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions.Dtos;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentSystem.Application.Assignments;

public interface IAssignmentService
{
    /// <summary>Unfiltered listing for administrative oversight.</summary>
    Task<PagedResult<AssignmentDto>> ListAllAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default);

    /// <summary>Assignments created by the calling teacher, at any status.</summary>
    Task<PagedResult<AssignmentDto>> ListMineAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default);

    Task<AssignmentDto> CreateAsync(
        CreateAssignmentRequest request, CancellationToken cancellationToken = default);

    Task<AssignmentDto> UpdateAsync(
        Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssignmentDto> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Submissions for one of the calling teacher's own assignments.</summary>
    Task<PagedResult<SubmissionDto>> ListSubmissionsAsync(
        Guid assignmentId, SubmissionListQuery query, CancellationToken cancellationToken = default);

    /// <summary>One assignment for a teacher or an admin. A teacher may only reach their
    /// own (rule 4); an admin may reach any, which is the whole point of oversight.</summary>
    Task<AssignmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Published assignments for the classes the calling student is enrolled in.</summary>
    Task<PagedResult<StudentAssignmentDto>> ListAvailableAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default);

    /// <summary>One assignment, as visible to the calling student.</summary>
    Task<StudentAssignmentDto> GetForStudentAsync(
        Guid id, CancellationToken cancellationToken = default);
}

public class AssignmentService : IAssignmentService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IResourceAuthorizer _authorizer;
    private readonly IClock _clock;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(
        IAppDbContext db,
        ICurrentUser currentUser,
        IResourceAuthorizer authorizer,
        IClock clock,
        ILogger<AssignmentService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _authorizer = authorizer;
        _clock = clock;
        _logger = logger;
    }

    // ---------------------------------------------------------------------------------
    // Reads
    // ---------------------------------------------------------------------------------

    public async Task<PagedResult<AssignmentDto>> ListAllAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(_db.Assignments.AsNoTracking(), query)
            .OrderByDescending(a => a.CreatedAt)
            .Select(Projections.ToAssignmentDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<PagedResult<AssignmentDto>> ListMineAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        // Scoped in SQL rather than fetched then filtered: the teacher's own assignments
        // are the only rows that ever leave the database.
        var mine = _db.Assignments
            .AsNoTracking()
            .Where(a => a.CreatedByTeacherId == teacherId);

        return await ApplyFilters(mine, query)
            .OrderByDescending(a => a.CreatedAt)
            .Select(Projections.ToAssignmentDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<AssignmentDto> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await LoadAsync(id, cancellationToken);

        // An admin reaches any assignment — that is the point of oversight. A teacher is
        // held to rule 4 and may only reach their own. Checked after the load and before
        // anything is returned, matching the other teacher reads.
        if (_currentUser.Role == UserRole.Teacher)
        {
            _authorizer.EnsureTeacherOwnsAssignment(_currentUser.RequireUserId(), assignment);
        }

        return await ProjectAsync(id, cancellationToken);
    }

    public async Task<PagedResult<SubmissionDto>> ListSubmissionsAsync(
        Guid assignmentId, SubmissionListQuery query, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var assignment = await LoadAsync(assignmentId, cancellationToken);

        // Business rule 4. Checked after the entity is loaded and before anything is
        // returned, so a teacher cannot read another teacher's submissions by id.
        _authorizer.EnsureTeacherOwnsAssignment(teacherId, assignment);

        var submissions = _db.Submissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId);

        if (query.Status is not null)
        {
            submissions = submissions.Where(s => s.Status == query.Status);
        }

        if (query.StudentId is not null)
        {
            submissions = submissions.Where(s => s.StudentId == query.StudentId);
        }

        return await submissions
            .OrderBy(s => s.Student.FullName)
            .Select(Projections.ToSubmissionDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    // ---------------------------------------------------------------------------------
    // Student reads — business rules 1 and 2
    // ---------------------------------------------------------------------------------

    public async Task<PagedResult<StudentAssignmentDto>> ListAvailableAsync(
        AssignmentListQuery query, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        // Both scoping rules are expressed as one composed query, so they execute as a
        // single SQL statement. Fetching broadly and filtering in memory would mean rows
        // the student may not see are read out of the database at all — and one forgotten
        // filter downstream would leak them.
        var available = _db.Assignments
            .AsNoTracking()
            .Where(a => a.Status == AssignmentStatus.Published)
            .Where(a => _db.Enrollments.Any(e =>
                e.StudentId == studentId && e.ClassCourseId == a.ClassCourseId));

        if (query.ClassCourseId is not null)
        {
            available = available.Where(a => a.ClassCourseId == query.ClassCourseId);
        }

        if (query.SubjectId is not null)
        {
            available = available.Where(a => a.SubjectId == query.SubjectId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            available = available.Where(a => a.Title.ToLower().Contains(term));
        }

        return await available
            .OrderBy(a => a.Deadline)
            .Select(ToStudentDto(studentId))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<StudentAssignmentDto> GetForStudentAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var studentId = _currentUser.RequireUserId();

        var assignment = await LoadAsync(id, cancellationToken);

        // Rule 1: a draft or archived assignment is answered as though it does not exist.
        // A 403 here would confirm that an assignment with this id is being prepared,
        // which is exactly what "students never see drafts" is meant to prevent.
        if (!AssignmentStatusPolicy.IsVisibleToStudents(assignment.Status))
        {
            throw new NotFoundException("Assignment", id);
        }

        // Rule 2: the assignment exists and is published, but belongs to another class.
        await _authorizer.EnsureStudentEnrolledInClassAsync(
            studentId, assignment.ClassCourseId, cancellationToken);

        return await _db.Assignments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(ToStudentDto(studentId))
            .SingleAsync(cancellationToken);
    }

    /// <summary>
    /// Projection including the calling student's own submission state. Built as an
    /// expression over a captured id so EF can translate the correlated lookup into the
    /// same SELECT rather than issuing one query per row.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Assignment, StudentAssignmentDto>>
        ToStudentDto(Guid studentId) =>
        a => new StudentAssignmentDto(
            a.Id,
            a.Title,
            a.Description,
            a.Deadline,
            a.MaxMarks,
            a.ClassCourseId,
            a.ClassCourse.Code,
            a.SubjectId,
            a.Subject.Name,
            a.CreatedByTeacher.FullName,
            a.AllowLateSubmission,
            a.AllowUpdateBeforeDeadline,
            a.Submissions.Any(s => s.StudentId == studentId),
            a.Submissions.Where(s => s.StudentId == studentId)
                .Select(s => (Guid?)s.Id).FirstOrDefault(),
            a.Submissions.Where(s => s.StudentId == studentId)
                .Select(s => (SubmissionStatus?)s.Status).FirstOrDefault(),
            a.Submissions.Where(s => s.StudentId == studentId)
                .Select(s => s.Marks).FirstOrDefault());

    // ---------------------------------------------------------------------------------
    // Writes
    // ---------------------------------------------------------------------------------

    public async Task<AssignmentDto> CreateAsync(
        CreateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();

        var subject = await _db.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == request.SubjectId, cancellationToken)
            ?? throw new NotFoundException("Subject", request.SubjectId);

        if (subject.ClassCourseId != request.ClassCourseId)
        {
            throw new ValidationFailedException(
                nameof(request.SubjectId),
                "That subject does not belong to the specified class.");
        }

        // Business rule 3. The teacher must hold an allocation for this exact
        // (subject, class) pair; holding one for the same subject in another class is not
        // enough. Throws 403 otherwise.
        await _authorizer.EnsureTeacherTeachesSubjectInClassAsync(
            teacherId, request.SubjectId, request.ClassCourseId, cancellationToken);

        var now = _clock.UtcNow;

        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            MaxMarks = request.MaxMarks,

            // Always created as a Draft. Publishing is a separate, deliberate act, which
            // is what makes business rule 1 meaningful — a half-written assignment cannot
            // reach students by accident.
            Status = AssignmentStatus.Draft,

            ClassCourseId = request.ClassCourseId,
            SubjectId = request.SubjectId,
            CreatedByTeacherId = teacherId,
            AllowLateSubmission = request.AllowLateSubmission,
            AllowUpdateBeforeDeadline = request.AllowUpdateBeforeDeadline,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} created draft assignment {AssignmentId}.", teacherId, assignment.Id);

        return await ProjectAsync(assignment.Id, cancellationToken);
    }

    public async Task<AssignmentDto> UpdateAsync(
        Guid id, UpdateAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();
        var assignment = await LoadAsync(id, cancellationToken);

        _authorizer.EnsureTeacherOwnsAssignment(teacherId, assignment);

        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description.Trim();
        assignment.Deadline = request.Deadline;
        assignment.MaxMarks = request.MaxMarks;
        assignment.AllowLateSubmission = request.AllowLateSubmission;
        assignment.AllowUpdateBeforeDeadline = request.AllowUpdateBeforeDeadline;
        assignment.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();
        var assignment = await LoadAsync(id, cancellationToken);

        _authorizer.EnsureTeacherOwnsAssignment(teacherId, assignment);

        // Deleting an assignment cascades to its submissions, so work students have
        // already handed in would go with it. Refused once anything has been submitted.
        var hasSubmissions = await _db.Submissions
            .AnyAsync(s => s.AssignmentId == id, cancellationToken);

        if (hasSubmissions)
        {
            throw new ResourceInUseException(
                "This assignment cannot be deleted because students have already submitted to it. "
                + "Archive it instead.");
        }

        _db.Assignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} deleted assignment {AssignmentId}.", teacherId, id);
    }

    public async Task<AssignmentDto> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentUser.RequireUserId();
        var assignment = await LoadAsync(id, cancellationToken);

        _authorizer.EnsureTeacherOwnsAssignment(teacherId, assignment);

        // Business rule 11. Only a Draft can be published; publishing something already
        // Published, or Archived, is a 409 rather than a quiet no-op — the caller should
        // learn that nothing changed.
        AssignmentStatusPolicy.EnsureCanPublish(assignment.Status);

        assignment.Status = AssignmentStatus.Published;
        assignment.UpdatedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Teacher {TeacherId} published assignment {AssignmentId}.", teacherId, id);

        return await ProjectAsync(id, cancellationToken);
    }

    // ---------------------------------------------------------------------------------

    private async Task<Assignment> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Assignments.SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
        ?? throw new NotFoundException("Assignment", id);

    private async Task<AssignmentDto> ProjectAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Assignments
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(Projections.ToAssignmentDto)
            .SingleAsync(cancellationToken);

    private static IQueryable<Assignment> ApplyFilters(
        IQueryable<Assignment> assignments, AssignmentListQuery query)
    {
        if (query.Status is not null)
        {
            assignments = assignments.Where(a => a.Status == query.Status);
        }

        if (query.ClassCourseId is not null)
        {
            assignments = assignments.Where(a => a.ClassCourseId == query.ClassCourseId);
        }

        if (query.SubjectId is not null)
        {
            assignments = assignments.Where(a => a.SubjectId == query.SubjectId);
        }

        if (query.TeacherId is not null)
        {
            assignments = assignments.Where(a => a.CreatedByTeacherId == query.TeacherId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            assignments = assignments.Where(a => a.Title.ToLower().Contains(term));
        }

        return assignments;
    }
}
