// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.TeacherAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Allocates teachers to subjects within classes. Admin-only: this table is the input to
/// the rule that decides where a teacher may create assignments, so letting teachers edit
/// it would let them grant themselves that permission.
/// </summary>
[ApiController]
[Route("api/v1/teacher-assignments")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class TeacherAssignmentsController : ControllerBase
{
    private readonly ITeacherAssignmentService _allocations;

    public TeacherAssignmentsController(ITeacherAssignmentService allocations) =>
        _allocations = allocations;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeacherAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TeacherAssignmentDto>>> List(
        [FromQuery] TeacherAssignmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _allocations.ListAsync(query, cancellationToken));
    }

    /// <summary>Allocates a teacher to teach a subject in a class.</summary>
    /// <response code="409">That allocation already exists.</response>
    /// <response code="422">The user is not a Teacher, or the subject is not part of that class.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TeacherAssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TeacherAssignmentDto>> Create(
        [FromBody] CreateTeacherAssignmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _allocations.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _allocations.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
