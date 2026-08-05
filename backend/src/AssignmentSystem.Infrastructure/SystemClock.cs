// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Common.Interfaces;

namespace AssignmentSystem.Infrastructure;

/// <inheritdoc cref="IClock"/>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
