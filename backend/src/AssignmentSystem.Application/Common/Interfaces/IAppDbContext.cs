// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// The persistence surface the Application layer is allowed to touch.
///
/// A repository per entity would add eight interfaces and eight implementations that each
/// forward one call to EF, which is ceremony rather than insulation. Exposing the DbSets
/// behind an interface keeps services substitutable in tests while letting queries be
/// written as composed LINQ — which matters here, because scoping rules like "Published
/// assignments for the classes this student is enrolled in" must execute as one SQL
/// statement rather than filtering an over-broad result set in memory.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<ClassCourse> ClassCourses { get; }

    DbSet<Subject> Subjects { get; }

    DbSet<Enrollment> Enrollments { get; }

    DbSet<TeacherAssignment> TeacherAssignments { get; }

    DbSet<Assignment> Assignments { get; }

    DbSet<Submission> Submissions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
