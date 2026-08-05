// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions;
using AssignmentSystem.Application.Submissions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>Submissions. Serves all three roles, so the role gate is per action.</summary>
[ApiController]
[Route("api/v1/submissions")]
[Produces("application/json")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly ISubmissionService _submissions;

    public SubmissionsController(ISubmissionService submissions) => _submissions = submissions;

    // ---------------------------------------------------------------------------------
    // Admin
    // ---------------------------------------------------------------------------------

    /// <summary>Lists every submission in the system, for administrative oversight.</summary>
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

    // ---------------------------------------------------------------------------------
    // Teacher
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Records marks and feedback, moving the submission to Graded.
    ///
    /// Accepts a submission that is Submitted, Late or UnderReview — entering marks is
    /// itself the review, so passing through UnderReview first is optional.
    /// </summary>
    /// <response code="403">The submission belongs to another teacher's assignment.</response>
    /// <response code="409">The submission is not in a gradeable state.</response>
    /// <response code="422">Marks fall outside 0 to the assignment's maximum.</response>
    [HttpPut("{id:guid}/grade")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SubmissionDto>> Grade(
        Guid id, [FromBody] GradeSubmissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _submissions.GradeAsync(id, request, cancellationToken));
    }

    /// <summary>Moves a submission through the review lifecycle explicitly.</summary>
    /// <response code="403">The submission belongs to another teacher's assignment.</response>
    /// <response code="409">That transition is not permitted from the current status.</response>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionDto>> ChangeStatus(
        Guid id, [FromBody] ChangeSubmissionStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _submissions.ChangeStatusAsync(id, request, cancellationToken));
    }
}
