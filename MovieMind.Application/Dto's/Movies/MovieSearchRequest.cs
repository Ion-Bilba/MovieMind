namespace MovieMind.Application.Dtos.Movies;

public class MovieSearchRequest
{
    public string? SearchTerm { get; set; }
    public Guid? GenreId { get; set; }
    public Guid? ActorId { get; set; }
    public int? ReleaseYear { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
