// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Persistence;
using AssignmentSystem.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AssignmentSystem.IntegrationTests.Persistence;

/// <summary>
/// The seeder runs on every API start, so "idempotent" is not a nicety — without it the
/// second `docker compose up` would fail on a duplicate key and take the API down with it.
/// </summary>
[Collection(PostgresCollection.Name)]
public class DbSeederTests
{
    private readonly PostgresFixture _fixture;

    public DbSeederTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Seeder_Is_Idempotent()
    {
        await using var db = _fixture.CreateContext();

        await DbSeeder.SeedAsync(db, _fixture.PasswordHasher, NullLogger.Instance);
        var afterFirst = await SnapshotAsync(db);

        // Second run must be a no-op rather than an error or a duplicate set of rows.
        await DbSeeder.SeedAsync(db, _fixture.PasswordHasher, NullLogger.Instance);
        var afterSecond = await SnapshotAsync(db);

        afterSecond.Should().BeEquivalentTo(afterFirst,
            "re-running the seeder must not add, duplicate or remove rows");
    }

    [Fact]
    public async Task Seeded_Passwords_Are_Hashed()
    {
        await using var db = _fixture.CreateContext();

        await DbSeeder.SeedAsync(db, _fixture.PasswordHasher, NullLogger.Instance);

        var hashes = await db.Users.AsNoTracking()
            .Select(u => u.PasswordHash)
            .ToListAsync();

        hashes.Should().NotBeEmpty();

        var plaintexts = new[]
        {
            DbSeeder.AdminPassword, DbSeeder.TeacherPassword, DbSeeder.StudentPassword
        };

        foreach (var plaintext in plaintexts)
        {
            hashes.Should().NotContain(plaintext,
                "no demo password may be stored in plaintext");
        }

        // Beyond "not equal to the plaintext": the stored value must actually verify,
        // which rules out a hash that is merely a different unusable string.
        var admin = await db.Users.AsNoTracking()
            .SingleAsync(u => u.Email == "admin@demo.test");

        _fixture.PasswordHasher.Verify(admin.PasswordHash, DbSeeder.AdminPassword)
            .Should().BeTrue("the seeded hash must verify against the documented password");

        _fixture.PasswordHasher.Verify(admin.PasswordHash, "WrongPassword@123")
            .Should().BeFalse("a wrong password must not verify");
    }

    [Fact]
    public async Task Seeder_Creates_Demo_Accounts_For_All_Three_Roles()
    {
        await using var db = _fixture.CreateContext();

        await DbSeeder.SeedAsync(db, _fixture.PasswordHasher, NullLogger.Instance);

        var demoAccounts = await db.Users.AsNoTracking()
            .Where(u => u.Email == "admin@demo.test"
                     || u.Email == "teacher@demo.test"
                     || u.Email == "student@demo.test")
            .ToDictionaryAsync(u => u.Email, u => u.Role);

        demoAccounts.Should().HaveCount(3);
        demoAccounts["admin@demo.test"].Should().Be(UserRole.Admin);
        demoAccounts["teacher@demo.test"].Should().Be(UserRole.Teacher);
        demoAccounts["student@demo.test"].Should().Be(UserRole.Student);
    }

    [Fact]
    public async Task Seeder_Creates_The_Fixtures_The_Business_Rules_Need()
    {
        await using var db = _fixture.CreateContext();

        await DbSeeder.SeedAsync(db, _fixture.PasswordHasher, NullLogger.Instance);

        // A draft assignment, so rule 1 (students never see Draft) can be tested for
        // rejection rather than only for the happy path.
        (await db.Assignments.AnyAsync(a => a.Status == AssignmentStatus.Draft))
            .Should().BeTrue("a Draft assignment is needed to test draft visibility");

        // A past-deadline assignment that accepts late work, for rule 5.
        (await db.Assignments.AnyAsync(a =>
                a.Deadline < DateTime.UtcNow && a.AllowLateSubmission))
            .Should().BeTrue("a past-deadline assignment is needed to test the late path");

        // Assignments in more than one class, for rule 2 (class scoping).
        (await db.Assignments.Select(a => a.ClassCourseId).Distinct().CountAsync())
            .Should().BeGreaterThan(1, "class scoping needs at least two classes in play");

        // An already-graded submission, so students have marks and feedback to read.
        (await db.Submissions.AnyAsync(s => s.Status == SubmissionStatus.Graded && s.Marks != null))
            .Should().BeTrue("a graded submission is needed to demonstrate marks and feedback");
    }

    private static async Task<Dictionary<string, int>> SnapshotAsync(AppDbContext db) => new()
    {
        ["Users"] = await db.Users.CountAsync(),
        ["ClassCourses"] = await db.ClassCourses.CountAsync(),
        ["Subjects"] = await db.Subjects.CountAsync(),
        ["Enrollments"] = await db.Enrollments.CountAsync(),
        ["TeacherAssignments"] = await db.TeacherAssignments.CountAsync(),
        ["Assignments"] = await db.Assignments.CountAsync(),
        ["Submissions"] = await db.Submissions.CountAsync()
    };
}
