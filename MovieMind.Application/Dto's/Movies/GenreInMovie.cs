namespace MovieMind.Application.Dtos.Movies;

public class GenreInMovie
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Reprezintă un gen asociat unui film în răspunsurile API.
/// </summary>