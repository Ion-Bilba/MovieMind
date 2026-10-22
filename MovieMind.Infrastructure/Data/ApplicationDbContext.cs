using Microsoft.EntityFrameworkCore;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<MovieActor> MovieActors => Set<MovieActor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

   
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.UserName).IsRequired().HasMaxLength(50);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(u => u.UserName).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ---------- Movie ----------
        modelBuilder.Entity<Movie>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.Property(m => m.Description).HasMaxLength(2000);
            e.Property(m => m.PosterUrl).HasMaxLength(500);
        });

        // ---------- Genre ----------
        modelBuilder.Entity<Genre>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(g => g.Name).IsUnique();
        });

        // ---------- Actor ----------
        modelBuilder.Entity<Actor>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Name).IsRequired().HasMaxLength(200);
        });

        // ---------- Movie <-> Genre (many-to-many, tabel intermediar automat) ----------
        modelBuilder.Entity<Movie>()
            .HasMany(m => m.Genres)
            .WithMany(g => g.Movies)
            .UsingEntity(j => j.ToTable("MovieGenres"));

        // ---------- Movie <-> Actor (many-to-many prin MovieActor) ----------
        modelBuilder.Entity<Movie>()
            .HasMany(m => m.Actors)
            .WithMany(a => a.Movies)
            .UsingEntity<MovieActor>(
                right => right
                    .HasOne(ma => ma.Actor)
                    .WithMany(a => a.MovieActors)
                    .HasForeignKey(ma => ma.ActorId),
                left => left
                    .HasOne(ma => ma.Movie)
                    .WithMany(m => m.MovieActors)
                    .HasForeignKey(ma => ma.MovieId),
                join =>
                {
                    join.HasKey(ma => new { ma.MovieId, ma.ActorId });
                    join.Property(ma => ma.CharacterName).HasMaxLength(200);
                });

      
        modelBuilder.Entity<Rating>(e =>
        {
            e.HasKey(r => r.Id);

            e.HasOne(r => r.User)
                .WithMany(u => u.Ratings)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Movie)
                .WithMany(m => m.Ratings)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            
            e.HasIndex(r => new { r.UserId, r.MovieId }).IsUnique();

        
            e.ToTable(t => t.HasCheckConstraint("CK_Ratings_Score", "[Score] >= 1 AND [Score] <= 5"));
        });
    }
}