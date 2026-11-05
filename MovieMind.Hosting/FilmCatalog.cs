using Microsoft.EntityFrameworkCore;
using MovieMind.Domain.Entities;
using MovieMind.Infrastructure.Persistence;

namespace MovieMind.Hosting;

public sealed class FilmCatalog(ApplicationDbContext db)
{
    public IQueryable<Movie> Query => db.Movies.AsSplitQuery().Include(m => m.Genres)
        .Include(m => m.MovieActors).ThenInclude(a => a.Actor);

    public async Task<FilmDocument> SaveAsync(FilmDocument input, CancellationToken cancellationToken)
    {
        var movie = await Query.SingleOrDefaultAsync(m => m.Id == input.Id, cancellationToken);
        if (movie is null)
        {
            movie = new Movie { Id = input.Id };
            db.Movies.Add(movie);
        }

        movie.Title = input.Title.Trim();
        movie.OriginalTitle = input.OriginalTitle?.Trim() ?? "";
        movie.ReleaseYear = input.Year;
        movie.Duration = input.Duration;
        movie.AgeRating = input.AgeRating?.Trim() ?? "";
        movie.CatalogRating = input.Rating;
        movie.Director = input.Director?.Trim() ?? "";
        movie.Composer = input.Composer?.Trim() ?? "";
        movie.Writers = input.Writers.Select(w => w.Trim()).ToList();
        movie.Description = input.Synopsis?.Trim() ?? "";
        movie.Awards = input.Awards?.Trim() ?? "";
        movie.Country = input.Country?.Trim() ?? "";
        movie.Language = input.Language?.Trim() ?? "";
        movie.TrailerUrl = input.TrailerUrl?.Trim() ?? "";
        movie.PosterUrl = input.Poster?.Trim() ?? "";
        movie.ArtPalette = input.Art.Palette;
        movie.ArtMotif = input.Art.Motif;
        movie.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var genreNames = input.Genres.Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var genres = await db.Genres.Where(g => genreNames.Contains(g.Name)).ToListAsync(cancellationToken);
        movie.Genres.Clear();
        foreach (var name in genreNames)
        {
            var genre = genres.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (genre is null)
            {
                genre = new Genre { Id = Guid.NewGuid(), Name = name };
                db.Genres.Add(genre);
            }
            movie.Genres.Add(genre);
        }

        var actorNames = input.Cast.Select(c => c.Name.Trim()).ToList();
        var actors = await db.Actors.Where(a => actorNames.Contains(a.Name)).ToListAsync(cancellationToken);
        var desiredLinks = new List<MovieActor>();
        foreach (var (member, position) in input.Cast.Select((member, position) => (member, position)))
        {
            var name = member.Name.Trim();
            var actor = actors.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (actor is null)
            {
                actor = new Actor { Id = Guid.NewGuid(), Name = name };
                db.Actors.Add(actor);
                actors.Add(actor);
            }
            var link = movie.MovieActors.FirstOrDefault(a => a.ActorId == actor.Id)
                ?? new MovieActor { Movie = movie, MovieId = movie.Id, Actor = actor, ActorId = actor.Id };
            link.CharacterName = member.Role.Trim();
            link.Position = position;
            desiredLinks.Add(link);
        }
        foreach (var link in movie.MovieActors.Where(a => !desiredLinks.Contains(a)).ToList())
            db.MovieActors.Remove(link);
        movie.MovieActors = desiredLinks;

        await db.SaveChangesAsync(cancellationToken);
        return FilmDocument.From(movie);
    }
}
