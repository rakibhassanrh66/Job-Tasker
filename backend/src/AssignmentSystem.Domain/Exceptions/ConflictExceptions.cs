// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// A resource with the same natural key already exists. Maps to 409.
///
/// The service checks before inserting so the caller gets this rather than a raw database
/// error, but the corresponding unique index is what actually guarantees it.
/// </summary>
public sealed class DuplicateResourceException : DomainException
{
    public DuplicateResourceException(string message) : base(message)
    {
    }

    public static DuplicateResourceException Email(string email) =>
        new($"A user with the email '{email}' already exists.");

    public static DuplicateResourceException ClassCode(string code) =>
        new($"A class with the code '{code}' already exists.");

    public static DuplicateResourceException Enrollment() =>
        new("This student is already enrolled in this class.");

    public static DuplicateResourceException TeacherAllocation() =>
        new("This teacher is already assigned to that subject in that class.");

    public override string Title => "Resource already exists";

    public override int StatusCode => 409;
}

/// <summary>
/// The resource cannot be deleted because other records still reference it. Maps to 409.
///
/// Deleting anyway would either orphan or destroy dependent data — a class still holding
/// enrolments and graded submissions, for instance. Refusing and saying why is the more
/// useful answer than cascading silently.
/// </summary>
public sealed class ResourceInUseException : DomainException
{
    public ResourceInUseException(string message) : base(message)
    {
    }

    public static ResourceInUseException Class(string what) =>
        new($"This class cannot be deleted because it still has {what}.");

    public static ResourceInUseException Subject(string what) =>
        new($"This subject cannot be deleted because it still has {what}.");

    public override string Title => "Resource in use";

    public override int StatusCode => 409;
}
