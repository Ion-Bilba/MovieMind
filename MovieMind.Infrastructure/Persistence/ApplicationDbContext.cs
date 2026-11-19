using Microsoft.EntityFrameworkCore;
using MovieMind.Domain.Entities;
using MovieMind.Application.Interfaces;

namespace MovieMind.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<UserPreferenceProfile> UserPreferenceProfiles => Set<UserPreferenceProfile>();
    public DbSet<UserGenrePreference> UserGenrePreferences => Set<UserGenrePreference>();
    public DbSet<MovieActor> MovieActors => Set<MovieActor>();

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected ApplicationDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
