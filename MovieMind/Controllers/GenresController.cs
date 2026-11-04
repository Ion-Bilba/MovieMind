using Microsoft.AspNetCore.Mvc;
using MovieMind.Application.Interfaces;
using MovieMind.Application.Dtos.Genres;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    public async Task<ActionResult<List<GenreDto>>> GetAll(CancellationToken cancellationToken)
    {
        var genres = await _genreService.GetAllAsync(cancellationToken);
        return Ok(genres);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GenreDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var genre = await _genreService.GetByIdAsync(id, cancellationToken);

        if (genre is null)
        {
            return NotFound();
        }

        return Ok(genre);
    }

    [HttpPost]
    public async Task<ActionResult<GenreDto>> Create(CreateGenreRequest request, CancellationToken cancellationToken)
    {
        if (await _genreService.NameExistsAsync(request.Name, null, cancellationToken))
        {
            return Conflict(new { message = "Există deja un gen cu acest nume." });
        }

        var created = await _genreService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GenreDto>> Update(
     Guid id,
     UpdateGenreRequest request,
     CancellationToken cancellationToken)
    {
        var existingGenre = await _genreService.GetByIdAsync(
            id, cancellationToken);

        if (existingGenre is null)
        {
            return NotFound();
        }

        if (await _genreService.NameExistsAsync(
            request.Name, id, cancellationToken))
        {
            return Conflict(new
            {
                message = "Există deja un gen cu acest nume."
            });
        }

        var updated = await _genreService.UpdateAsync(
            id, request, cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _genreService.DeleteAsync(id, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}