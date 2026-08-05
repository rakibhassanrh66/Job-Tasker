// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Application.Auth;
using AssignmentSystem.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    /// <summary>Rate limiter policy name, applied to the credential-accepting endpoints.</summary>
    public const string AuthRateLimitPolicy = "auth";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Exchanges email and password for an access token and a refresh token.</summary>
    /// <response code="200">Authenticated.</response>
    /// <response code="401">Unknown email, wrong password, or deactivated account.</response>
    /// <response code="422">The request itself was invalid.</response>
    /// <response code="429">Too many attempts from this address.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    /// <summary>Exchanges a valid refresh token for a new token pair. The presented token
    /// is revoked as part of the exchange.</summary>
    /// <response code="200">Rotated.</response>
    /// <response code="401">Token unknown, expired, or already revoked.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitPolicy)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    /// <summary>Returns the authenticated caller's own profile.</summary>
    /// <response code="200">The caller's profile.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfile>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetCurrentUserAsync(cancellationToken));
    }
}
