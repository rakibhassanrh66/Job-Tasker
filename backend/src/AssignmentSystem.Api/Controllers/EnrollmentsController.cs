// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

/// <summary>
/// Class membership. Admin-only for the same reason as teacher allocations: enrolment is
/// what decides which assignments a student can see, so a student who could enrol
/// themselves could read any class they liked.
/// </summary>
[ApiController]
[Route("api/v1/enrollments")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EnrollmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EnrollmentDto>>> List(
        [FromQuery] EnrollmentListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _enrollments.ListAsync(query, cancellationToken));
    }

    /// <response code="409">The student is already enrolled in that class.</response>
    /// <response code="422">The user is not a Student.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EnrollmentDto>> Create(
        [FromBody] CreateEnrollmentRequest request, CancellationToken cancellationToken)
    {
        var created = await _enrollments.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _enrollments.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
