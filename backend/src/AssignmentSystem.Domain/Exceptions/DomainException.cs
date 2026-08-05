// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// Base for every rule violation the domain can raise. The API's exception middleware
/// translates these into ProblemDetails responses, so each subclass owns the HTTP
/// status and title it should surface as. Controllers never map status codes by hand.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>Short human-readable summary, used as the ProblemDetails "title".</summary>
    public abstract string Title { get; }

    /// <summary>The HTTP status this violation corresponds to.</summary>
    public abstract int StatusCode { get; }
}
