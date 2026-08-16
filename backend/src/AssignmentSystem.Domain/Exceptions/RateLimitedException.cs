// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

namespace AssignmentSystem.Domain.Exceptions;

/// <summary>
/// Too many failed authentication attempts for the account. Maps to 429.
///
/// Kept distinct from <see cref="InvalidCredentialsException"/> on purpose: while a wrong
/// password is indistinguishable from an unknown email, a lockout is a different state and
/// the client is expected to surface it as "try again later" rather than "wrong password".
/// </summary>
public sealed class RateLimitedException : DomainException
{
    public RateLimitedException() : base("Too many failed attempts. Try again later.")
    {
    }

    public RateLimitedException(TimeSpan retryAfter) : base(
        $"Too many failed attempts. Try again in {Math.Max(1, (int)retryAfter.TotalSeconds)} seconds.")
    {
    }

    public override string Title => "Too many attempts";

    public override int StatusCode => 429;
}
