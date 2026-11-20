using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Services;
using MovieMind.Domain.Entities;
using MovieMind.Domain.Enums;
using MovieMind.Infrastructure.Persistence;
using Xunit;

namespace MovieMind.Application.Tests.Recommendations;

public sealed class RecommendationServiceTests
{
    private static readonly Guid Action = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Drama = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TargetUser = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid SimilarUser = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid OtherUser = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid NewUser = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid SeenMovie = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ActionMovie = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid DramaMovie = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task GetForUserAsync_CombinesClusterRatingsAndContentAndExcludesRatedMovies()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var context = CreateContext(connection);
        await SeedRecommendationScenarioAsync(context);
        var service = new RecommendationService(context, new UserPreferencesService(context));

        var result = await service.GetForUserAsync(TargetUser, 10);

        Assert.NotNull(result);
        Assert.Equal("Hybrid", result.Strategy);
        Assert.DoesNotContain(result.Items, item => item.MovieId == SeenMovie);
        Assert.Equal(ActionMovie, result.Items[0].MovieId);
        Assert.NotNull(result.Items[0].CollaborativeScore);
        Assert.Equal("Action", Assert.Single(result.Items[0].Genres));
    }

    [Fact]
    public async Task GetForUserAsync_UsesPopularityForANewUser()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var context = CreateContext(connection);
        await SeedNewUserScenarioAsync(context);
        var service = new RecommendationService(context, new UserPreferencesService(context));

        var result = await service.GetForUserAsync(NewUser, 10);

        Assert.NotNull(result);
        Assert.Equal("Popularity", result.Strategy);
        Assert.Equal("Highly Rated", result.Items[0].Title);
        Assert.Equal(0.9, result.Items[0].Score, 10);
        Assert.Null(result.Items[0].CollaborativeScore);
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsNullWhenUserDoesNotExist()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var context = CreateContext(connection);
        var service = new RecommendationService(context, new UserPreferencesService(context));

        Assert.Null(await service.GetForUserAsync(Guid.NewGuid(), 10));
    }

    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        return connection;
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedRecommendationScenarioAsync(ApplicationDbContext context)
    {
        var target = User(TargetUser, "target");
        var similar = User(SimilarUser, "similar");
        var other = User(OtherUser, "other");
        var action = new Genre { Id = Action, Name = "Action" };
        var drama = new Genre { Id = Drama, Name = "Drama" };
        var seen = Movie(SeenMovie, "Already Seen", 5, action);
        var actionMovie = Movie(ActionMovie, "Action Pick", 8, action);
        var dramaMovie = Movie(DramaMovie, "Drama Pick", 9, drama);

        context.Users.AddRange(target, similar, other);
        context.Genres.AddRange(action, drama);
        context.Movies.AddRange(seen, actionMovie, dramaMovie);
        context.Ratings.AddRange(
            Rating(target, seen, 9),
            Rating(similar, actionMovie, 9),
            Rating(similar, dramaMovie, 2),
            Rating(other, actionMovie, 0),
            Rating(other, dramaMovie, 10));
        await context.SaveChangesAsync();
    }

    private static async Task SeedNewUserScenarioAsync(ApplicationDbContext context)
    {
        context.Users.Add(User(NewUser, "new-user"));
        var action = new Genre { Id = Action, Name = "Action" };
        var drama = new Genre { Id = Drama, Name = "Drama" };
        context.Genres.AddRange(action, drama);
        context.Movies.AddRange(
            Movie(ActionMovie, "Highly Rated", 9, action),
            Movie(DramaMovie, "Lower Rated", 6, drama));
        await context.SaveChangesAsync();
    }

    private static User User(Guid id, string name) => new()
    {
        Id = id,
        UserName = name,
        Email = $"{name}@example.test",
        PasswordHash = "test-hash",
        Role = UserRole.User
    };

    private static Movie Movie(Guid id, string title, double catalogRating, Genre genre) => new()
    {
        Id = id,
        Title = title,
        Description = string.Empty,
        ReleaseYear = 2025,
        Duration = 100,
        CatalogRating = catalogRating,
        Genres = [genre]
    };

    private static Rating Rating(User user, Movie movie, double score) => new()
    {
        Id = Guid.NewGuid(),
        User = user,
        UserId = user.Id,
        Movie = movie,
        MovieId = movie.Id,
        Score = score
    };
}