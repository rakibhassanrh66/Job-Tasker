// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Assignments;
using AssignmentSystem.Application.Submissions;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.UnitTests.Assignments;

/// <summary>
/// Business rule 11, checked over the entire transition space rather than a few examples.
///
/// The interesting failure is not "an allowed move was refused" — that shows up
/// immediately in normal use. It is "a move nobody intended was quietly permitted", and
/// only enumerating every pair can rule that out.
/// </summary>
public class AssignmentStatusPolicyTests
{
    private static readonly (AssignmentStatus From, AssignmentStatus To)[] Allowed =
    {
        (AssignmentStatus.Draft, AssignmentStatus.Published),
        (AssignmentStatus.Draft, AssignmentStatus.Archived),
        (AssignmentStatus.Published, AssignmentStatus.Archived)
    };

    public static TheoryData<AssignmentStatus, AssignmentStatus> AllPairs()
    {
        var data = new TheoryData<AssignmentStatus, AssignmentStatus>();

        foreach (var from in Enum.GetValues<AssignmentStatus>())
        {
            foreach (var to in Enum.GetValues<AssignmentStatus>())
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Only_The_Listed_Transitions_Are_Permitted(AssignmentStatus from, AssignmentStatus to)
    {
        var expected = Allowed.Contains((from, to));

        AssignmentStatusPolicy.CanTransition(from, to).Should().Be(expected,
            $"{from} -> {to} should be {(expected ? "allowed" : "refused")}");
    }

    [Fact]
    public void Publishing_A_Draft_Is_Allowed()
    {
        var act = () => AssignmentStatusPolicy.EnsureCanPublish(AssignmentStatus.Draft);

        act.Should().NotThrow();
    }

    [Fact]
    public void Republishing_A_Published_Assignment_Is_Refused()
    {
        // Chosen over a silent no-op so the caller learns nothing changed.
        var act = () => AssignmentStatusPolicy.EnsureCanPublish(AssignmentStatus.Published);

        act.Should().Throw<InvalidStatusTransitionException>()
            .Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public void Publishing_An_Archived_Assignment_Is_Refused()
    {
        var act = () => AssignmentStatusPolicy.EnsureCanPublish(AssignmentStatus.Archived);

        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Theory]
    [InlineData(AssignmentStatus.Draft, false)]
    [InlineData(AssignmentStatus.Published, true)]
    [InlineData(AssignmentStatus.Archived, false)]
    public void Only_Published_Assignments_Are_Visible_To_Students(
        AssignmentStatus status, bool visible)
    {
        // Business rule 1, stated as a single predicate the student queries rely on.
        AssignmentStatusPolicy.IsVisibleToStudents(status).Should().Be(visible);
    }
}

/// <summary>Business rule 10, over the entire transition space.</summary>
public class SubmissionStatusPolicyTests
{
    private static readonly (SubmissionStatus From, SubmissionStatus To)[] Allowed =
    {
        (SubmissionStatus.Submitted, SubmissionStatus.UnderReview),
        (SubmissionStatus.Submitted, SubmissionStatus.Graded),
        (SubmissionStatus.Submitted, SubmissionStatus.Returned),

        (SubmissionStatus.Late, SubmissionStatus.UnderReview),
        (SubmissionStatus.Late, SubmissionStatus.Graded),
        (SubmissionStatus.Late, SubmissionStatus.Returned),

        (SubmissionStatus.UnderReview, SubmissionStatus.Graded),
        (SubmissionStatus.UnderReview, SubmissionStatus.Returned),

        (SubmissionStatus.Graded, SubmissionStatus.Returned)
    };

    public static TheoryData<SubmissionStatus, SubmissionStatus> AllPairs()
    {
        var data = new TheoryData<SubmissionStatus, SubmissionStatus>();

        foreach (var from in Enum.GetValues<SubmissionStatus>())
        {
            foreach (var to in Enum.GetValues<SubmissionStatus>())
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void Only_The_Listed_Transitions_Are_Permitted(SubmissionStatus from, SubmissionStatus to)
    {
        var expected = Allowed.Contains((from, to));

        SubmissionStatusPolicy.CanTransition(from, to).Should().Be(expected,
            $"{from} -> {to} should be {(expected ? "allowed" : "refused")}");
    }

    [Fact]
    public void Nothing_Transitions_Into_Late()
    {
        // Late is set once, at creation, when a student submits after the deadline on an
        // assignment that allows it. It is a fact about when the work arrived, not a stage
        // of review, so no later transition may produce it.
        foreach (var from in Enum.GetValues<SubmissionStatus>())
        {
            SubmissionStatusPolicy.CanTransition(from, SubmissionStatus.Late)
                .Should().BeFalse($"{from} -> Late must never be permitted");
        }
    }

    [Fact]
    public void No_Status_Transitions_To_Itself()
    {
        foreach (var status in Enum.GetValues<SubmissionStatus>())
        {
            SubmissionStatusPolicy.CanTransition(status, status)
                .Should().BeFalse($"{status} -> {status} is a no-op and should be refused");
        }
    }

    [Fact]
    public void Returned_Is_Terminal()
    {
        foreach (var to in Enum.GetValues<SubmissionStatus>())
        {
            SubmissionStatusPolicy.CanTransition(SubmissionStatus.Returned, to)
                .Should().BeFalse($"Returned -> {to} must not be permitted");
        }
    }

    [Theory]
    [InlineData(SubmissionStatus.Submitted, true)]
    [InlineData(SubmissionStatus.Late, true)]
    [InlineData(SubmissionStatus.UnderReview, true)]
    [InlineData(SubmissionStatus.Graded, false)]
    [InlineData(SubmissionStatus.Returned, false)]
    public void Grading_Is_Allowed_From_The_Pre_Grade_States(
        SubmissionStatus status, bool canGrade)
    {
        // Grading straight from Submitted or Late is deliberate: entering marks is the
        // review, so requiring a separate UnderReview call first would be ceremony.
        SubmissionStatusPolicy.CanGrade(status).Should().Be(canGrade);
    }

    [Fact]
    public void Grading_An_Already_Graded_Submission_Is_Refused()
    {
        var act = () => SubmissionStatusPolicy.EnsureCanGrade(SubmissionStatus.Graded);

        act.Should().Throw<InvalidStatusTransitionException>()
            .Which.StatusCode.Should().Be(409);
    }
}
