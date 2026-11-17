using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieMind.Domain.Enums;
using MovieMind.Application.Dtos.Actors;
using MovieMind.Application.Interfaces;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActorsController : ControllerBase
{
    private readonly IActorService _actorService;

    public ActorsController(IActorService actorService)
    {
        _actorService = actorService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActorDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        return Ok(await _actorService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ActorDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actor = await _actorService.GetByIdAsync(id, cancellationToken);

        return actor is null ? NotFound() : Ok(actor);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ActorDto>> Create(
        CreateActor request,
        CancellationToken cancellationToken)
    {
        if (await _actorService.NameExistsAsync(
                request.Name, cancellationToken: cancellationToken))
        {
            return Conflict(new { message = "Există deja un actor cu acest nume." });
        }

        var created = await _actorService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ActorDto>> Update(
        Guid id,
        UpdateActor request,
        CancellationToken cancellationToken)
    {
        var existing = await _actorService.GetByIdAsync(id, cancellationToken);

        if (existing is null)
        {
            return NotFound();
        }

        if (await _actorService.NameExistsAsync(
                request.Name, id, cancellationToken))
        {
            return Conflict(new { message = "Există deja un actor cu acest nume." });
        }

        var updated = await _actorService.UpdateAsync(id, request, cancellationToken);

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _actorService.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}