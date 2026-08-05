// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Assignments;
using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Assignments. Unlike the admin-only controllers, this one serves all three roles, so the
/// gate is per action rather than on the class — and each action states its own role.
///
/// Route order matters here: the literal segments must be declared before the
/// "{id:guid}" route, and that route carries a guid constraint, so a request for
/// /assignments/available can never be matched as an id.
/// </summary>
[ApiController]
[Route("api/v1/assignments")]
[Produces("application/json")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignments;

    public AssignmentsController(IAssignmentService assignments) => _assignments = assignments;

    /// <summary>
    /// Lists every assignment in the system, for administrative oversight. Read-only.
    /// </summary>
    /// <response code="200">A page of assignments.</response>
    /// <response code="403">The caller is not an administrator.</response>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> ListAll(
        [FromQuery] AssignmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _assignments.ListAllAsync(query, cancellationToken));
    }
}
