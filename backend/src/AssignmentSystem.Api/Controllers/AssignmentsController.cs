// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Assignments;
using AssignmentSystem.Application.Assignments.Dtos;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Submissions.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Assignments. This controller serves all three roles, so the role gate is per action
/// rather than on the class, and each action declares its own.
///
/// The literal routes ("mine", and later "available") are declared before "{id:guid}", and
/// that route carries a guid constraint — so a request for /assignments/mine can never be
/// matched as an assignment id.
/// </summary>
[ApiController]
[Route("api/v1/assignments")]
[Produces("application/json")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignments;

    public AssignmentsController(IAssignmentService assignments) => _assignments = assignments;

    // ---------------------------------------------------------------------------------
    // Admin
    // ---------------------------------------------------------------------------------

    /// <summary>Lists every assignment in the system, for administrative oversight.</summary>
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

    // ---------------------------------------------------------------------------------
    // Teacher
    // ---------------------------------------------------------------------------------

    /// <summary>Lists assignments created by the calling teacher, at any status.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(PagedResult<AssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AssignmentDto>>> ListMine(
        [FromQuery] AssignmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _assignments.ListMineAsync(query, cancellationToken));
    }

    /// <summary>
    /// Creates an assignment as a Draft. The caller must be allocated to teach the given
    /// subject in the given class.
    /// </summary>
    /// <response code="201">Created, with status Draft.</response>
    /// <response code="403">The caller does not teach that subject in that class.</response>
    /// <response code="422">The subject does not belong to that class, or the request is invalid.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AssignmentDto>> Create(
        [FromBody] CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _assignments.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(ListMine), new { id = created.Id }, created);
    }

    /// <summary>Updates one of the caller's own assignments.</summary>
    /// <response code="403">The assignment belongs to a different teacher.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentDto>> Update(
        Guid id, [FromBody] UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _assignments.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes one of the caller's own assignments, provided nothing has been submitted.</summary>
    /// <response code="403">The assignment belongs to a different teacher.</response>
    /// <response code="409">Students have already submitted to it.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _assignments.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Publishes a Draft assignment, making it visible to the enrolled students.
    /// </summary>
    /// <response code="403">The assignment belongs to a different teacher.</response>
    /// <response code="409">The assignment is not a Draft — already Published, or Archived.</response>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssignmentDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _assignments.PublishAsync(id, cancellationToken));
    }

    /// <summary>Lists submissions for one of the caller's own assignments.</summary>
    /// <response code="403">The assignment belongs to a different teacher.</response>
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(PagedResult<SubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<SubmissionDto>>> ListSubmissions(
        Guid id, [FromQuery] SubmissionListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _assignments.ListSubmissionsAsync(id, query, cancellationToken));
    }
}
