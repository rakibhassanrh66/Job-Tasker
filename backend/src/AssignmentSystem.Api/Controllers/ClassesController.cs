// Copyright (c) 2026 Rakib Hassan. Submitted for candidacy evaluation only.
// Not licensed for production or commercial use. See LICENSE. sig:a24a5edb253940aa

using AssignmentSystem.Api.Authorization;
using AssignmentSystem.Application.Classes;
using AssignmentSystem.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSystem.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Produces("application/json")]
[Authorize(Roles = Roles.Admin)]
public class ClassesController : ControllerBase
{
    private readonly IClassCourseService _classes;

    public ClassesController(IClassCourseService classes) => _classes = classes;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClassCourseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ClassCourseDto>>> List(
        [FromQuery] ClassCourseListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _classes.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassCourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassCourseDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _classes.GetAsync(id, cancellationToken));
    }

    /// <response code="409">A class with that code already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClassCourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassCourseDto>> Create(
        [FromBody] CreateClassCourseRequest request, CancellationToken cancellationToken)
    {
        var created = await _classes.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClassCourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClassCourseDto>> Update(
        Guid id, [FromBody] UpdateClassCourseRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _classes.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a class, provided nothing still references it.</summary>
    /// <response code="409">The class still has enrolments, subjects or assignments.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _classes.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
