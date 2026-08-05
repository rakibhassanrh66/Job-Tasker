// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssignmentSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ClassCourse> ClassCourses => Set<ClassCourse>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ApplyUtcDateTimeConversion(modelBuilder);
    }

    /// <summary>
    /// Forces every DateTime in the model through a UTC converter.
    ///
    /// Two problems this solves. Npgsql refuses to write a non-UTC DateTime to a
    /// timestamptz column and throws at runtime. And values read back from Npgsql arrive
    /// with Kind=Utc, but values that never round-tripped through the database carry
    /// Kind=Unspecified — so a deadline comparison could silently compare an
    /// Unspecified local time against a UTC "now". Normalising in one place means every
    /// DateTime in the system is unambiguously UTC, rather than relying on each call
    /// site to remember.
    /// </summary>
    private static void ApplyUtcDateTimeConversion(ModelBuilder modelBuilder)
    {
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            toDb => toDb.Kind == DateTimeKind.Local
                ? toDb.ToUniversalTime()
                : DateTime.SpecifyKind(toDb, DateTimeKind.Utc),
            fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

        var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
            toDb => toDb.HasValue
                ? (toDb.Value.Kind == DateTimeKind.Local
                    ? toDb.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(toDb.Value, DateTimeKind.Utc))
                : toDb,
            fromDb => fromDb.HasValue
                ? DateTime.SpecifyKind(fromDb.Value, DateTimeKind.Utc)
                : fromDb);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(utcConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableUtcConverter);
                }
            }
        }
    }
}
