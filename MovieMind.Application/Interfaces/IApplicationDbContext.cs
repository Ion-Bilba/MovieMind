using Microsoft.EntityFrameworkCore;
using MovieMind.Domain.Entities;

namespace MovieMind.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Movie> Movies { get; }
    DbSet<Genre> Genres { get; }
    DbSet<Actor> Actors { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<UserPreferenceProfile> UserPreferenceProfiles { get; }
    DbSet<UserGenrePreference> UserGenrePreferences { get; }
    DbSet<MovieActor> MovieActors { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
