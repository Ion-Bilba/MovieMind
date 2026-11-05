using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Genres;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public class GenreService : IGenreService
{
    private readonly IApplicationDbContext _context;

    public GenreService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GenreDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<GenreDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Genres
            .AsNoTracking()
            .Where(g => g.Id == id)
            .Select(g => new GenreDto { Id = g.Id, Name = g.Name })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim();

        return await _context.Genres
            .AnyAsync(g => g.Name == normalized && (excludeId == null || g.Id != excludeId), cancellationToken);
    }

    public async Task<GenreDto> CreateAsync(CreateGenreRequest request, CancellationToken cancellationToken = default)
    {
        var genre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim()
        };

        _context.Genres.Add(genre);
        await _context.SaveChangesAsync(cancellationToken);

        return new GenreDto { Id = genre.Id, Name = genre.Name };
    }

    public async Task<GenreDto?> UpdateAsync(Guid id, UpdateGenreRequest request, CancellationToken cancellationToken = default)
    {
        var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (genre is null)
        {
            return null;
        }

        genre.Name = request.Name.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return new GenreDto { Id = genre.Id, Name = genre.Name };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (genre is null)
        {
            return false;
        }

        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}