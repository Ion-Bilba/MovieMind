using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieMind.Domain.Enums;
using MovieMind.Application.Dtos.Movies;
using MovieMind.Application.Interfaces;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MovieDto>>> GetAll(
        [FromQuery] MovieSearchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _movieService.SearchAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovieDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var movie = await _movieService.GetByIdAsync(id, cancellationToken);

        return movie is null ? NotFound() : Ok(movie);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MovieDto>> Create(
        CreateMovie request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _movieService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<MovieDto>> Update(
        Guid id,
        UpdateMovie request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _movieService.UpdateAsync(
                id, request, cancellationToken);

            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _movieService.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}