using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieMind.Domain.Entities;

namespace MovieMind.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movies");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.ReleaseYear)
            .IsRequired();

        builder.Property(m => m.Duration)
            .IsRequired();

        builder.Property(m => m.AverageRating)
            .IsRequired();

        builder.Property(m => m.RatingCount)
            .IsRequired();

        builder.Property(m => m.PosterUrl)
            .HasMaxLength(500);

        builder.HasMany(m => m.Genres)
            .WithMany(g => g.Movies)
            .UsingEntity<Dictionary<string, object>>(
                "MovieGenres",
                right => right
                    .HasOne<Genre>()
                    .WithMany()
                    .HasForeignKey("GenreId"),
                left => left
                    .HasOne<Movie>()
                    .WithMany()
                    .HasForeignKey("MovieId"),
                join =>
                {
                    join.ToTable("MovieGenres");
                    join.HasKey("MovieId", "GenreId");
                });

        builder.HasMany(m => m.Actors)
            .WithMany(a => a.Movies)
            .UsingEntity<MovieActor>(
                right => right
                    .HasOne(ma => ma.Actor)
                    .WithMany(a => a.MovieActors)
                    .HasForeignKey(ma => ma.ActorId),
                left => left
                    .HasOne(ma => ma.Movie)
                    .WithMany(m => m.MovieActors)
                    .HasForeignKey(ma => ma.MovieId));
    }
}