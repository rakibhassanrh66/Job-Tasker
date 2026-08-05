// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions;
using AssignmentSystem.Application.Submissions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Submissions. Serves all three roles, so the role gate is per action.
/// </summary>
[ApiController]
[Route("api/v1/submissions")]
[Produces("application/json")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissions;

    public SubmissionsController(ISubmissionService submissions) => _submissions = submissions;

    /// <summary>
    /// Lists every submission in the system, for administrative oversight. Read-only.
    /// </summary>
    /// <response code="403">The caller is not an administrator.</response>
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<SubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListAll(
        [FromQuery] SubmissionListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _submissions.ListAllAsync(query, cancellationToken));
    }
}
