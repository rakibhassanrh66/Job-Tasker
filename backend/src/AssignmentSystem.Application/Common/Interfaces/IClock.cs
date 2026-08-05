// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// The current time, injected rather than read from DateTime.UtcNow directly.
///
/// Deadlines and token expiry are the two places this system makes decisions about time.
/// Testing "submitting one second after the deadline is rejected" against the real clock
/// would mean either sleeping or seeding data relative to now and hoping the test runs
/// fast enough. With this, the test simply states what time it is.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
