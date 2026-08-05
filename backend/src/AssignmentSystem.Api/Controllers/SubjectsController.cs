// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Application.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/v1/subjects")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjects;

    public SubjectsController(ISubjectService subjects) => _subjects = subjects;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SubjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<SubjectDto>>> List(
        [FromQuery] SubjectListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _subjects.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _subjects.GetAsync(id, cancellationToken));
    }

    /// <response code="404">The specified class does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDto>> Create(
        [FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var created = await _subjects.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDto>> Update(
        Guid id, [FromBody] UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _subjects.UpdateAsync(id, request, cancellationToken));
    }

    /// <response code="409">The subject still has assignments or teacher allocations.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _subjects.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
