using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Movies;
using MovieMind.Application.Interfaces;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Services;

public class MovieService : IMovieService
{
    private readonly IApplicationDbContext _context;

    public MovieService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<MovieDto>> SearchAsync(
        MovieSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = _context.Movies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();
            query = query.Where(movie =>
                movie.Title.Contains(searchTerm) ||
                movie.Description.Contains(searchTerm));
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(movie => movie.Genres.Any(genre => genre.Id == request.GenreId.Value));
        }

        if (request.ActorId.HasValue)
        {
            query = query.Where(movie => movie.MovieActors.Any(link => link.ActorId == request.ActorId.Value));
        }

        if (request.ReleaseYear.HasValue)
        {
            query = query.Where(movie => movie.ReleaseYear == request.ReleaseYear.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        var movies = await query
            .Include(movie => movie.Genres)
            .Include(movie => movie.MovieActors)
                .ThenInclude(movieActor => movieActor.Actor)
            .OrderBy(movie => movie.Title)
            .ThenBy(movie => movie.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<MovieDto>
        {
            Items = movies.Select(MapToDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<MovieDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .Include(movie => movie.Genres)
            .Include(movie => movie.MovieActors)
                .ThenInclude(movieActor => movieActor.Actor)
            .FirstOrDefaultAsync(movie => movie.Id == id, cancellationToken);

        return movie is null ? null : MapToDto(movie);
    }

    public async Task<MovieDto> CreateAsync(
        CreateMovie request,
        CancellationToken cancellationToken = default)
    {
        var genres = await GetGenresAsync(
            request.GenreIds,
            cancellationToken);

        var actorLinks = await GetActorLinksAsync(
            request.Actors,
            cancellationToken);

        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ReleaseYear = request.ReleaseYear,
            Duration = request.Duration,
            PosterUrl = request.PosterUrl.Trim(),
            Genres = genres,
            MovieActors = actorLinks
        };

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(movie);
    }

    public async Task<MovieDto?> UpdateAsync(
        Guid id,
        UpdateMovie request,
        CancellationToken cancellationToken = default)
    {
        var movie = await _context.Movies
            .Include(item => item.Genres)
            .Include(item => item.MovieActors)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (movie is null)
        {
            return null;
        }

        var genres = await GetGenresAsync(
            request.GenreIds,
            cancellationToken);

        var actorLinks = await GetActorLinksAsync(
            request.Actors,
            cancellationToken);

        movie.Title = request.Title.Trim();
        movie.Description = request.Description.Trim();
        movie.ReleaseYear = request.ReleaseYear;
        movie.Duration = request.Duration;
        movie.PosterUrl = request.PosterUrl.Trim();

        movie.Genres.Clear();
        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        movie.MovieActors.Clear();
        foreach (var actorLink in actorLinks)
        {
            movie.MovieActors.Add(actorLink);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movie = await _context.Movies
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (movie is null)
        {
            return false;
        }

        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<List<Genre>> GetGenresAsync(
        IEnumerable<Guid> genreIds,
        CancellationToken cancellationToken)
    {
        var ids = genreIds.Distinct().ToList();

        var genres = await _context.Genres
            .Where(genre => ids.Contains(genre.Id))
            .ToListAsync(cancellationToken);

        if (genres.Count != ids.Count)
        {
            throw new ArgumentException("Unul sau mai multe ID-uri de gen nu există.");
        }

        return genres;
    }

    private async Task<List<MovieActor>> GetActorLinksAsync(
        IEnumerable<ActorInMovie> actors,
        CancellationToken cancellationToken)
    {
        var requestedActors = actors.ToList();
        var actorIds = requestedActors.Select(actor => actor.Id).Distinct().ToList();

        var foundActors = await _context.Actors
            .Where(actor => actorIds.Contains(actor.Id))
            .ToDictionaryAsync(actor => actor.Id, cancellationToken);

        if (foundActors.Count != actorIds.Count)
        {
            throw new ArgumentException("Unul sau mai multe ID-uri de actori nu există.");
        }

        return requestedActors
            .Select(actor => new MovieActor
            {
                ActorId = actor.Id,
                Actor = foundActors[actor.Id],
                CharacterName = actor.CharacterName?.Trim()
            })
            .ToList();
    }

    private static MovieDto MapToDto(Movie movie)
    {
        return new MovieDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Description = movie.Description,
            ReleaseYear = movie.ReleaseYear,
            Duration = movie.Duration,
            PosterUrl = movie.PosterUrl,
            AverageRating = movie.AverageRating,
            RatingCount = movie.RatingCount,
            Genres = movie.Genres
                .Select(genre => new GenreInMovie
                {
                    Id = genre.Id,
                    Name = genre.Name
                })
                .ToList(),
            Actors = movie.MovieActors
                .Select(movieActor => new ActorInMovie
                {
                    Id = movieActor.ActorId,
                    Name = movieActor.Actor.Name,
                    CharacterName = movieActor.CharacterName
                })
                .ToList()
        };
    }
}
