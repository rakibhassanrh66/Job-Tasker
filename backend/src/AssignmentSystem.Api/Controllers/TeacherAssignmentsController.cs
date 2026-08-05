// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.TeacherAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Allocates teachers to subjects within classes. Every action but `mine` is admin-only:
/// this table is the input to the rule that decides where a teacher may create assignments,
/// so letting teachers edit it would let them grant themselves that permission. Reading
/// their own row is different, and is the only way a teacher can discover where they may work.
///
/// The role gate is declared per action rather than on the class. It has to be: multiple
/// [Authorize] attributes are combined, not overridden, so a class-level Admin plus an
/// action-level Teacher would demand both roles at once and refuse everybody.
/// </summary>
[ApiController]
[Route("api/v1/teacher-assignments")]
[Produces("application/json")]
[Authorize]
public class TeacherAssignmentsController : ControllerBase
{
    private readonly ITeacherAssignmentService _allocations;

    public TeacherAssignmentsController(ITeacherAssignmentService allocations) =>
        _allocations = allocations;

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(typeof(PagedResult<TeacherAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TeacherAssignmentDto>>> List(
        [FromQuery] TeacherAssignmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _allocations.ListAsync(query, cancellationToken));
    }

    /// <summary>
    /// The calling teacher's own allocations — the (subject, class) pairs they may set work
    /// in.
    /// </summary>
    /// <remarks>
    /// The subject and class catalogues are admin-only, so without this a teacher would have
    /// no way to name a valid subject and class when creating an assignment. Scoped to the
    /// caller from the token, so it grants sight of nobody else's allocations.
    /// </remarks>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Teacher)]
    [ProducesResponseType(typeof(PagedResult<TeacherAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<TeacherAssignmentDto>>> ListMine(
        [FromQuery] TeacherAssignmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _allocations.ListMineAsync(query, cancellationToken));
    }

    /// <summary>Allocates a teacher to teach a subject in a class.</summary>
    /// <response code="409">That allocation already exists.</response>
    /// <response code="422">The user is not a Teacher, or the subject is not part of that class.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
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
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _allocations.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
