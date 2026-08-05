// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Enums;

/// <summary>
/// Lifecycle of an assignment. Students may only ever see <see cref="Published"/>
/// (business rule 1). Values start at 1 so default(AssignmentStatus) is not Draft.
/// </summary>
public enum AssignmentStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
