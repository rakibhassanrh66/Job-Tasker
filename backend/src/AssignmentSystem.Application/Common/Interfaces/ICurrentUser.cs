// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Domain.Enums;

namespace AssignmentSystem.Application.Common.Interfaces;

/// <summary>
/// The authenticated caller, read from the validated JWT.
///
/// Every ownership decision resolves identity through this and never from a route
/// parameter or request body. That distinction is the whole point: if a service trusted a
/// studentId sent by the client, any student could read another student's submission just
/// by changing the number, and the role check would still pass.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Null when the request is unauthenticated.</summary>
    Guid? UserId { get; }

    string? Email { get; }

    UserRole? Role { get; }

    bool IsAuthenticated { get; }

    /// <summary>The caller's id, or throws if unauthenticated. For use in code paths that
    /// are already behind [Authorize], where anonymous access is a programming error
    /// rather than an expected condition.</summary>
    Guid RequireUserId();
}
