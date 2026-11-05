using System.ComponentModel.DataAnnotations;
using MovieMind.Domain.Entities;

namespace MovieMind.Hosting;

public sealed class FilmDocument : IValidatableObject
{
    public Guid Id { get; set; }
    [Required, StringLength(140)] public string Title { get; set; } = "";
    [StringLength(140)] public string OriginalTitle { get; set; } = "";
    [Range(1888, 2100)] public int Year { get; set; }
    [Range(0, 999)] public int Duration { get; set; }
    [StringLength(20)] public string AgeRating { get; set; } = "";
    [Range(0, 10)] public double Rating { get; set; }
    public List<string> Genres { get; set; } = [];
    [StringLength(140)] public string Director { get; set; } = "";
    [StringLength(120)] public string Composer { get; set; } = "";
    public List<string> Writers { get; set; } = [];
    public List<CastMember> Cast { get; set; } = [];
    [StringLength(3000)] public string Synopsis { get; set; } = "";
    [StringLength(800)] public string Awards { get; set; } = "";
    [StringLength(140)] public string Country { get; set; } = "";
    [StringLength(140)] public string Language { get; set; } = "";
    [StringLength(2000)] public string TrailerUrl { get; set; } = "";
    [StringLength(2000)] public string Poster { get; set; } = "";
    public Artwork Art { get; set; } = new();
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    internal static bool IsPosterPath(string? value) => value is not null &&
        System.Text.RegularExpressions.Regex.IsMatch(value,
            @"^/media/posters/[a-f0-9]{32}\.(jpg|png|webp)$");

    private static bool IsWebUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == "https" || uri.Scheme == "http");

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Id == Guid.Empty) yield return new("A valid film ID is required.", [nameof(Id)]);
        if (!double.IsFinite(Rating)) yield return new("Rating must be finite.", [nameof(Rating)]);
        if (Genres is null || Genres.Count > 8 || Genres.Any(g => string.IsNullOrWhiteSpace(g) || g.Length > 100))
            yield return new("Choose up to eight genres, each up to 100 characters.", [nameof(Genres)]);
        if (Writers is null || Writers.Count > 10 || Writers.Any(w => string.IsNullOrWhiteSpace(w) || w.Length > 120))
            yield return new("Enter up to ten writers, each up to 120 characters.", [nameof(Writers)]);
        if (Cast is null || Cast.Count > 40 || Cast.Any(c => c is null || string.IsNullOrWhiteSpace(c.Name) || c.Name.Length > 80 || c.Role is null || c.Role.Length > 80))
            yield return new("Enter up to forty cast members with names and roles up to 80 characters.", [nameof(Cast)]);
        else if (Cast.Select(c => c.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Cast.Count)
            yield return new("Each actor may appear only once; combine their roles on one line.", [nameof(Cast)]);
        if (!string.IsNullOrEmpty(TrailerUrl) && !IsWebUrl(TrailerUrl))
            yield return new("Trailer link must use HTTP or HTTPS.", [nameof(TrailerUrl)]);
        if (!string.IsNullOrEmpty(Poster) && !IsWebUrl(Poster) && !IsPosterPath(Poster))
            yield return new("Upload a poster or provide an HTTP or HTTPS image URL.", [nameof(Poster)]);
        if (Art is null || Art.Palette < 0 || Art.Palette > 9 ||
            !new[] { "rings", "orb", "dunes", "sun", "stairs", "rain", "bands", "grid", "window", "stars", "spot" }.Contains(Art.Motif))
            yield return new("Choose a valid artwork palette and motif.", [nameof(Art)]);
    }

    internal static FilmDocument From(Movie movie) => new()
    {
        Id = movie.Id, Title = movie.Title, OriginalTitle = movie.OriginalTitle,
        Year = movie.ReleaseYear, Duration = movie.Duration, AgeRating = movie.AgeRating,
        Rating = movie.CatalogRating, Director = movie.Director, Composer = movie.Composer,
        Writers = movie.Writers, Synopsis = movie.Description, Awards = movie.Awards,
        Country = movie.Country, Language = movie.Language, TrailerUrl = movie.TrailerUrl,
        Poster = movie.PosterUrl, Art = new() { Palette = movie.ArtPalette, Motif = movie.ArtMotif },
        CreatedAt = movie.CreatedAt, UpdatedAt = movie.UpdatedAt,
        Genres = movie.Genres.OrderBy(g => g.Name).Select(g => g.Name).ToList(),
        Cast = movie.MovieActors.OrderBy(a => a.Position)
            .Select(a => new CastMember { Name = a.Actor.Name, Role = a.CharacterName ?? "" }).ToList()
    };
}

public sealed class CastMember
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

public sealed class Artwork
{
    public int Palette { get; set; }
    public string Motif { get; set; } = "rings";
}
