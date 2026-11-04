using System.ComponentModel.DataAnnotations;

namespace MovieMind.Application.Dtos.Movies;

public class UpdateMovie
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(1888, 2100)]
    public int ReleaseYear { get; set; }

    [Range(1, 1000)]
    public int Duration { get; set; }

    public string PosterUrl { get; set; } = string.Empty;

    public List<Guid> GenreIds { get; set; } = new();

    public List<ActorInMovie> Actors { get; set; } = new();
}