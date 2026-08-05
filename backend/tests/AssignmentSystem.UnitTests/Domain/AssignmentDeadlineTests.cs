// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.UnitTests.Domain;

/// <summary>
/// The deadline comparison underpins business rules 5 and 7, so it is pinned down here
/// in isolation — including the boundary, where off-by-one errors live.
/// </summary>
public class AssignmentDeadlineTests
{
    private static readonly DateTime Deadline =
        new(2026, 8, 20, 23, 59, 0, DateTimeKind.Utc);

    [Fact]
    public void Before_The_Deadline_Is_Not_Past()
    {
        var assignment = new Assignment { Deadline = Deadline };

        assignment.IsPastDeadline(Deadline.AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void After_The_Deadline_Is_Past()
    {
        var assignment = new Assignment { Deadline = Deadline };

        assignment.IsPastDeadline(Deadline.AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void Exactly_On_The_Deadline_Is_Not_Past()
    {
        // The boundary is inclusive: a submission landing on the stroke of the deadline
        // is on time. Stated explicitly so a later refactor cannot quietly flip it.
        var assignment = new Assignment { Deadline = Deadline };

        assignment.IsPastDeadline(Deadline).Should().BeFalse();
    }
}
