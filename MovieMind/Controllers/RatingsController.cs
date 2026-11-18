using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieMind.Application.Dtos.Ratings;
using MovieMind.Application.Interfaces;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpGet("movies/{movieId:guid}/ratings")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMovieRatings(
        Guid movieId,
        CancellationToken cancellationToken)
    {
        var ratings = await _ratingService.GetMovieRatingsAsync(movieId, cancellationToken);
        return ratings is null ? NotFound() : Ok(ratings);
    }

    [HttpGet("ratings/me")]
    [Authorize]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _ratingService.GetUserRatingsAsync(userId.Value, cancellationToken));
    }

    [HttpPost("movies/{movieId:guid}/ratings")]
    [Authorize]
    public async Task<IActionResult> Create(
        Guid movieId,
        [FromBody] RateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var rating = await _ratingService.CreateAsync(
                movieId, userId.Value, request.Score, cancellationToken);

            return rating is null
                ? NotFound(new { message = "Filmul sau utilizatorul nu a fost găsit." })
                : StatusCode(StatusCodes.Status201Created, rating);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Evaluarea nu a putut fi adăugată. Reîncarcă și încearcă din nou." });
        }
    }

    [HttpPut("movies/{movieId:guid}/ratings/me")]
    [Authorize]
    public async Task<IActionResult> UpdateMine(
        Guid movieId,
        [FromBody] RateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var rating = await _ratingService.UpdateMineAsync(
            movieId, userId.Value, request.Score, cancellationToken);

        return rating is null
            ? NotFound(new { message = "Nu ai încă o evaluare pentru acest film." })
            : Ok(rating);
    }

    private Guid? GetCurrentUserId()
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }
}
