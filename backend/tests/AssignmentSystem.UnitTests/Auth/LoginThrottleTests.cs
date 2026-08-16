// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Auth;
using FluentAssertions;
using Xunit;

namespace AssignmentSystem.UnitTests.Auth;

public class LoginThrottleTests
{
    private static readonly DateTime Start = new(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TestClock(DateTime start)
    {
        public DateTime Now { get; set; } = start;
    }

    private static (LoginThrottle throttle, TestClock clock) Create(
        TimeSpan? window = null, int maxFailures = 5, int maxAttemptsPerWindow = 1000)
    {
        var clock = new TestClock(Start);
        var throttle = new LoginThrottle(
            maxFailures,
            window ?? TimeSpan.FromMinutes(15),
            maxAttemptsPerWindow,
            TimeSpan.FromMinutes(15),
            () => clock.Now);
        return (throttle, clock);
    }

    [Fact]
    public void No_Lockout_Before_Threshold_Is_Reached()
    {
        var (throttle, _) = Create();

        throttle.RecordFailure("a@b.c").Should().BeNull();
        throttle.RecordFailure("a@b.c").Should().BeNull();
        throttle.RemainingLockout("a@b.c").Should().BeNull();
    }

    [Fact]
    public void Lockout_Begins_At_The_Fifth_Failure()
    {
        var (throttle, clock) = Create();

        for (var i = 0; i < 4; i++)
        {
            throttle.RecordFailure("a@b.c");
            clock.Now = clock.Now.AddSeconds(1);
        }

        // Fifth failure crosses the threshold: 5-minute first rung.
        var remaining = throttle.RecordFailure("a@b.c");

        remaining.Should().NotBeNull();
        remaining!.Value.TotalMinutes.Should().BeApproximately(5, 0.5);
    }

    [Fact]
    public void Escalates_Through_The_Backoff_Ladder()
    {
        var (throttle, clock) = Create();

        TimeSpan? last = null;
        for (var i = 0; i < 8; i++)
        {
            last = throttle.RecordFailure("a@b.c");
            clock.Now = clock.Now.AddSeconds(1);
        }

        // Rungs: 5m, 15m, 1h, 6h — the 8th failure caps at the 6-hour rung.
        last.Should().NotBeNull();
        last!.Value.TotalHours.Should().BeGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void Locked_Account_Reports_Remaining_Lockout()
    {
        var (throttle, clock) = Create();

        for (var i = 0; i < 5; i++)
        {
            throttle.RecordFailure("a@b.c");
            clock.Now = clock.Now.AddSeconds(1);
        }

        var remaining = throttle.RemainingLockout("a@b.c");
        remaining.Should().NotBeNull();
        remaining!.Value.TotalSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Lockout_Expires_And_Allows_Retry()
    {
        var (throttle, clock) = Create();

        for (var i = 0; i < 5; i++)
        {
            throttle.RecordFailure("a@b.c");
            clock.Now = clock.Now.AddSeconds(1);
        }

        clock.Now = clock.Now.AddHours(7);

        throttle.RemainingLockout("a@b.c").Should().BeNull();

        // A failure after expiry starts a fresh window.
        throttle.RecordFailure("a@b.c").Should().BeNull();
    }

    [Fact]
    public void Success_Clears_The_Failed_Attempt_State()
    {
        var (throttle, clock) = Create();

        for (var i = 0; i < 4; i++)
        {
            throttle.RecordFailure("a@b.c");
            clock.Now = clock.Now.AddSeconds(1);
        }

        throttle.RecordSuccess("a@b.c");

        // A successful login must reset the counter so a typo does not keep the account locked.
        throttle.RecordFailure("a@b.c").Should().BeNull();
    }

    [Fact]
    public void Failures_Outside_The_Sliding_Window_Do_Not_Accumulate()
    {
        var (throttle, clock) = Create();

        for (var i = 0; i < 4; i++)
        {
            throttle.RecordFailure("a@b.c");
            // Each failure falls outside the previous 15-minute window.
            clock.Now = clock.Now.AddMinutes(16);
        }

        // Only the latest failure still counts, so no lockout yet.
        throttle.RecordFailure("a@b.c").Should().BeNull();
    }

    // ---------------------------------------------------------------------------------
    // Per-account attempt window
    // ---------------------------------------------------------------------------------

    [Fact]
    public void Attempts_Within_The_Limit_Are_Allowed()
    {
        var (throttle, _) = Create(maxAttemptsPerWindow: 3);

        throttle.AllowAttempt("a@b.c").Should().BeTrue();
        throttle.AllowAttempt("a@b.c").Should().BeTrue();
        throttle.AllowAttempt("a@b.c").Should().BeTrue();
    }

    [Fact]
    public void Attempts_Beyond_The_Limit_Are_Refused()
    {
        var (throttle, _) = Create(maxAttemptsPerWindow: 3);

        throttle.AllowAttempt("a@b.c");
        throttle.AllowAttempt("a@b.c");
        throttle.AllowAttempt("a@b.c");

        throttle.AllowAttempt("a@b.c").Should().BeFalse(
            "the fourth attempt within the window must be refused");
    }

    [Fact]
    public void Attempt_Window_Resets_After_The_Period_Elapses()
    {
        var (throttle, clock) = Create(maxAttemptsPerWindow: 3);

        for (var i = 0; i < 3; i++)
        {
            throttle.AllowAttempt("a@b.c");
        }

        clock.Now = clock.Now.AddMinutes(16);

        throttle.AllowAttempt("a@b.c").Should().BeTrue(
            "a fresh attempt window begins once the period has elapsed");
    }

    [Fact]
    public void Attempt_Limits_Are_Per_Account_Not_Global()
    {
        var (throttle, _) = Create(maxAttemptsPerWindow: 2);

        throttle.AllowAttempt("a@b.c");
        throttle.AllowAttempt("a@b.c");

        // A different account is unaffected by the first account's usage.
        throttle.AllowAttempt("x@y.z").Should().BeTrue();
    }
}
