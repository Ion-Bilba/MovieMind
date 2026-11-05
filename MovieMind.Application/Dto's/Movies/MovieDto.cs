namespace MovieMind.Application.Dtos.Movies;

public class MovieDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public int Duration { get; set; }
    public string PosterUrl { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }

    public List<GenreInMovie> Genres { get; set; } = new();
    public List<ActorInMovie> Actors { get; set; } = new();
}