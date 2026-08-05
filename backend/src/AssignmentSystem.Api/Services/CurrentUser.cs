// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Domain.Enums;
using AssignmentSystem.Infrastructure.Auth;

namespace AssignmentSystem.Api.Services;

/// <summary>
/// Reads the caller's identity from the validated JWT on the current request.
///
/// The values come from claims the authentication middleware has already verified against
/// the signing key. Nothing here reads the request body, route or query string — that is
/// what makes ownership checks trustworthy.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? Principal?.FindFirstValue(JwtTokenService.UserIdClaimType)
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public UserRole? Role
    {
        get
        {
            var raw = Principal?.FindFirstValue(JwtTokenService.RoleClaimType)
                      ?? Principal?.FindFirstValue(ClaimTypes.Role);

            return Enum.TryParse<UserRole>(raw, ignoreCase: true, out var role) ? role : null;
        }
    }

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException(
            "No authenticated user on this request. This code path must sit behind [Authorize].");
}
