namespace MovieMind.Application.Dtos.Movies;

public class ActorInMovie
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CharacterName { get; set; }
}

/// <summary>
/// Reprezintă un actor asociat unui film în răspunsurile API.
/// Include numele rolului interpretat, dacă este cunoscut.
/// </summary>