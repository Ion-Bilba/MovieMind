using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Actors;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public class ActorService : IActorService
{
    private readonly IApplicationDbContext _context;

    public ActorService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ActorDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Actors
            .AsNoTracking()
            .OrderBy(actor => actor.Name)
            .Select(actor => new ActorDto
            {
                Id = actor.Id,
                Name = actor.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ActorDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Actors
            .AsNoTracking()
            .Where(actor => actor.Id == id)
            .Select(actor => new ActorDto
            {
                Id = actor.Id,
                Name = actor.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return await _context.Actors.AnyAsync(
            actor => actor.Name == normalizedName &&
                     (excludeId == null || actor.Id != excludeId),
            cancellationToken);
    }

    public async Task<ActorDto> CreateAsync(
        CreateActor request,
        CancellationToken cancellationToken = default)
    {
        var actor = new Actor
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        _context.Actors.Add(actor);
        await _context.SaveChangesAsync(cancellationToken);

        return new ActorDto
        {
            Id = actor.Id,
            Name = actor.Name
        };
    }

    public async Task<ActorDto?> UpdateAsync(
        Guid id,
        UpdateActor request,
        CancellationToken cancellationToken = default)
    {
        var actor = await _context.Actors
            .FirstOrDefaultAsync(actor => actor.Id == id, cancellationToken);

        if (actor is null)
        {
            return null;
        }

        actor.Name = request.Name.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return new ActorDto
        {
            Id = actor.Id,
            Name = actor.Name
        };
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var actor = await _context.Actors
            .FirstOrDefaultAsync(actor => actor.Id == id, cancellationToken);

        if (actor is null)
        {
            return false;
        }

        _context.Actors.Remove(actor);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}