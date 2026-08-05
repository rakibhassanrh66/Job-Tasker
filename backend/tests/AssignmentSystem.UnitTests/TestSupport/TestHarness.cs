// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSystem.UnitTests.TestSupport;

/// <summary>A clock the test controls, so "before the deadline" and "after the deadline"
/// are stated rather than waited for.</summary>
public class TestClock : IClock
{
    public TestClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

/// <summary>A stand-in current user, since these tests run with no HTTP context.</summary>
public class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException("No authenticated user in this test.");
}

public static class TestDb
{
    /// <summary>
    /// An isolated in-memory context per test.
    ///
    /// These are service-logic tests: they check which branch a rule takes, not what the
    /// database enforces. Constraints, unique indexes and foreign keys are deliberately
    /// tested elsewhere, against real PostgreSQL, because this provider ignores all three.
    /// </summary>
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"unit-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}
