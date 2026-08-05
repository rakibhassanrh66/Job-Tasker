// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Users;
using AssignmentSystem.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// User administration. The role gate sits on the controller so every action inherits it —
/// a new action cannot be added without authorization by forgetting an attribute.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    /// <summary>Lists users, filterable by role, active state and a name/email search.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _users.ListAsync(query, cancellationToken));
    }

    /// <summary>Fetches a single user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _users.GetAsync(id, cancellationToken));
    }

    /// <summary>Creates a user with the given role.</summary>
    /// <response code="409">The email is already registered.</response>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _users.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Updates a user's name or active state.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _users.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>
    /// Deactivates a user. Users are never hard-deleted: they are referenced by the
    /// assignments they authored and the submissions they made or graded.
    /// </summary>
    /// <response code="409">Attempting to deactivate your own account.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _users.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }
}
