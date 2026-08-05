// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Subjects;

public record SubjectDto(
    Guid Id,
    string Name,
    string Code,
    Guid ClassCourseId,
    string ClassCourseName,
    string ClassCourseCode);

public record CreateSubjectRequest(string Name, string Code, Guid ClassCourseId);

public record UpdateSubjectRequest(string Name, string Code);

public class SubjectListQuery : PagedQuery
{
    public Guid? ClassCourseId { get; set; }

    public string? Search { get; set; }
}

public interface ISubjectService
{
    Task<PagedResult<SubjectDto>> ListAsync(SubjectListQuery query, CancellationToken cancellationToken = default);

    Task<SubjectDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SubjectDto> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default);

    Task<SubjectDto> UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class SubjectService : ISubjectService
{
    private readonly IAppDbContext _db;

    public SubjectService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<SubjectDto>> ListAsync(
        SubjectListQuery query, CancellationToken cancellationToken = default)
    {
        var subjects = _db.Subjects.AsNoTracking();

        if (query.ClassCourseId is not null)
        {
            subjects = subjects.Where(s => s.ClassCourseId == query.ClassCourseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            subjects = subjects.Where(s => s.Name.ToLower().Contains(term) || s.Code.ToLower().Contains(term));
        }

        return await subjects
            .OrderBy(s => s.ClassCourse.Code)
            .ThenBy(s => s.Code)
            .Select(s => new SubjectDto(
                s.Id, s.Name, s.Code, s.ClassCourseId, s.ClassCourse.Name, s.ClassCourse.Code))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<SubjectDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.Subjects
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubjectDto(
                s.Id, s.Name, s.Code, s.ClassCourseId, s.ClassCourse.Name, s.ClassCourse.Code))
            .SingleOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException("Subject", id);
    }

    public async Task<SubjectDto> CreateAsync(
        CreateSubjectRequest request, CancellationToken cancellationToken = default)
    {
        // A subject belongs to exactly one class, so a missing parent is a 404 about the
        // class rather than a foreign key error about the subject.
        var classExists = await _db.ClassCourses
            .AnyAsync(c => c.Id == request.ClassCourseId, cancellationToken);

        if (!classExists)
        {
            throw new NotFoundException("Class", request.ClassCourseId);
        }

        var entity = new Subject
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = request.Code.Trim().ToUpperInvariant(),
            ClassCourseId = request.ClassCourseId
        };

        _db.Subjects.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<SubjectDto> UpdateAsync(
        Guid id, UpdateSubjectRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Subjects.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Subject", id);

        entity.Name = request.Name.Trim();
        entity.Code = request.Code.Trim().ToUpperInvariant();

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Subjects.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Subject", id);

        if (await _db.Assignments.AnyAsync(a => a.SubjectId == id, cancellationToken))
        {
            throw ResourceInUseException.Subject("assignments");
        }

        if (await _db.TeacherAssignments.AnyAsync(t => t.SubjectId == id, cancellationToken))
        {
            throw ResourceInUseException.Subject("teacher allocations");
        }

        _db.Subjects.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
