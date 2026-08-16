// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.Collections.Concurrent;

namespace AssignmentSystem.Application.Auth;

/// <summary>
/// Per-account authentication throttling, layered on top of the per-address rate limiter
/// so credential stuffing is bounded per account even when it comes from many different
/// source addresses.
///
/// Two independent controls live here:
///  - an attempt window capping how many login attempts one account may make in a period,
///    and
///  - an escalating (exponential) failure lockout that kicks in after repeated failures.
///
/// In-memory by design for this single-instance deployment. A multi-instance deployment
/// would move the state into a shared store (Redis, or the database) — the interface is
/// small enough to swap without touching the auth service.
/// </summary>
public interface ILoginThrottle
{
    /// <summary>Remaining lockout for the account, or <see langword="null"/> if not locked.</summary>
    TimeSpan? RemainingLockout(string email);

    /// <summary>
    /// Counts one login attempt for the account and reports whether it is within the
    /// per-account attempt window.
    /// </summary>
    bool AllowAttempt(string email);

    /// <summary>Records a failed attempt. Returns the new lockout, or <see langword="null"/>.</summary>
    TimeSpan? RecordFailure(string email);

    /// <summary>Clears the account's failed-attempt state after a successful login.</summary>
    void RecordSuccess(string email);
}

public sealed class LoginThrottle : ILoginThrottle
{
    // Escalating lockout ladder (5 min, 15 min, 1 hr, 6 hr) — each further failure beyond
    // the first lockout moves the account one rung up, capped at the last rung.
    private static readonly TimeSpan[] BackoffLadder =
    {
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6)
    };

    private readonly int _maxFailures;
    private readonly TimeSpan _window;
    private readonly int _maxAttemptsPerWindow;
    private readonly TimeSpan _attemptWindow;
    private readonly Func<DateTime> _clock;
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    private sealed record Entry(
        int Failures,
        DateTime WindowStartUtc,
        DateTime LockoutUntilUtc,
        int Attempts,
        DateTime AttemptWindowStartUtc);

    public LoginThrottle(
        int maxFailures,
        TimeSpan window,
        int maxAttemptsPerWindow,
        TimeSpan attemptWindow,
        Func<DateTime>? clock = null)
    {
        _maxFailures = Math.Max(1, maxFailures);
        _window = window;
        _maxAttemptsPerWindow = Math.Max(1, maxAttemptsPerWindow);
        _attemptWindow = attemptWindow;
        // Injectable clock keeps the ladder testable without sleeping.
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public TimeSpan? RemainingLockout(string email)
    {
        var key = Key(email);
        var now = _clock();

        if (_entries.TryGetValue(key, out var entry) && entry.LockoutUntilUtc > now)
        {
            return entry.LockoutUntilUtc - now;
        }

        return null;
    }

    public bool AllowAttempt(string email)
    {
        var key = Key(email);
        var now = _clock();

        var entry = _entries.AddOrUpdate(
            key,
            _ => NewEntry(now, attempts: 1),
            (_, existing) =>
            {
                var attempts = existing.Attempts;
                var attemptStart = existing.AttemptWindowStartUtc;

                // Sliding attempt window: only attempts inside it count.
                if (now > attemptStart + _attemptWindow)
                {
                    attempts = 0;
                    attemptStart = now;
                }

                return existing with
                {
                    Attempts = attempts + 1,
                    AttemptWindowStartUtc = attemptStart
                };
            });

        return entry.Attempts <= _maxAttemptsPerWindow;
    }

    public TimeSpan? RecordFailure(string email)
    {
        var key = Key(email);
        var now = _clock();

        var entry = _entries.AddOrUpdate(
            key,
            _ => NewEntry(now, attempts: 0) with { Failures = 1 },
            (_, existing) =>
            {
                // Sliding window: only failures inside the window count.
                if (now > existing.WindowStartUtc + _window)
                {
                    return NewEntry(now, attempts: existing.Attempts) with { Failures = 1 };
                }

                var failures = existing.Failures + 1;
                var lockoutUntil = existing.LockoutUntilUtc;

                if (lockoutUntil > now)
                {
                    // Already locked — escalate to the next rung.
                    lockoutUntil = now + Backoff(failures);
                }
                else if (failures >= _maxFailures)
                {
                    lockoutUntil = now + Backoff(failures);
                }

                return existing with
                {
                    Failures = failures,
                    LockoutUntilUtc = lockoutUntil
                };
            });

        return entry.LockoutUntilUtc > now ? entry.LockoutUntilUtc - now : null;
    }

    public void RecordSuccess(string email) => _entries.TryRemove(Key(email), out _);

    private static Entry NewEntry(DateTime now, int attempts) =>
        new(0, now, DateTime.MinValue, attempts, now);

    private static string Key(string email) => email.Trim().ToLowerInvariant();

    private TimeSpan Backoff(int failures) =>
        BackoffLadder[Math.Clamp(failures - _maxFailures, 0, BackoffLadder.Length - 1)];
}
