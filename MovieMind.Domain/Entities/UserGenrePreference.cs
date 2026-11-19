namespace MovieMind.Domain.Entities;

public sealed class UserGenrePreference
{
    public Guid UserId { get; set; }
    public UserPreferenceProfile Profile { get; set; } = null!;
    public Guid GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    public double Score { get; set; }
}