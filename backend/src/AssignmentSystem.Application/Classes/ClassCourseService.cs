// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Classes;

public record ClassCourseDto(
    Guid Id,
    string Name,
    string Code,
    int SubjectCount,
    int EnrollmentCount);

public record CreateClassCourseRequest(string Name, string Code);

public record UpdateClassCourseRequest(string Name, string Code);

public class ClassCourseListQuery : PagedQuery
{
    public string? Search { get; set; }
}

public interface IClassCourseService
{
    Task<PagedResult<ClassCourseDto>> ListAsync(ClassCourseListQuery query, CancellationToken cancellationToken = default);

    Task<ClassCourseDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClassCourseDto> CreateAsync(CreateClassCourseRequest request, CancellationToken cancellationToken = default);

    Task<ClassCourseDto> UpdateAsync(Guid id, UpdateClassCourseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class ClassCourseService : IClassCourseService
{
    private readonly IAppDbContext _db;

    public ClassCourseService(IAppDbContext db) => _db = db;

    public async Task<PagedResult<ClassCourseDto>> ListAsync(
        ClassCourseListQuery query, CancellationToken cancellationToken = default)
    {
        var classes = _db.ClassCourses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            classes = classes.Where(c => c.Name.ToLower().Contains(term) || c.Code.ToLower().Contains(term));
        }

        // Counts are projected into the query so the database aggregates them, rather than
        // loading each class's children just to call .Count on them.
        return await classes
            .OrderBy(c => c.Code)
            .Select(c => new ClassCourseDto(
                c.Id, c.Name, c.Code, c.Subjects.Count, c.Enrollments.Count))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ClassCourseDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.ClassCourses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClassCourseDto(
                c.Id, c.Name, c.Code, c.Subjects.Count, c.Enrollments.Count))
            .SingleOrDefaultAsync(cancellationToken);

        return item ?? throw new NotFoundException("Class", id);
    }

    public async Task<ClassCourseDto> CreateAsync(
        CreateClassCourseRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.ClassCourses.AnyAsync(c => c.Code == code, cancellationToken))
        {
            throw DuplicateResourceException.ClassCode(code);
        }

        var entity = new ClassCourse
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Code = code
        };

        _db.ClassCourses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new ClassCourseDto(entity.Id, entity.Name, entity.Code, 0, 0);
    }

    public async Task<ClassCourseDto> UpdateAsync(
        Guid id, UpdateClassCourseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClassCourses.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Class", id);

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _db.ClassCourses.AnyAsync(c => c.Code == code && c.Id != id, cancellationToken))
        {
            throw DuplicateResourceException.ClassCode(code);
        }

        entity.Name = request.Name.Trim();
        entity.Code = code;

        await _db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClassCourses.SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Class", id);

        // Checked explicitly so the caller gets a 409 that says what is in the way, rather
        // than a foreign key violation surfacing as an opaque 500.
        if (await _db.Enrollments.AnyAsync(e => e.ClassCourseId == id, cancellationToken))
        {
            throw ResourceInUseException.Class("enrolled students");
        }

        if (await _db.Subjects.AnyAsync(s => s.ClassCourseId == id, cancellationToken))
        {
            throw ResourceInUseException.Class("subjects");
        }

        if (await _db.Assignments.AnyAsync(a => a.ClassCourseId == id, cancellationToken))
        {
            throw ResourceInUseException.Class("assignments");
        }

        _db.ClassCourses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
