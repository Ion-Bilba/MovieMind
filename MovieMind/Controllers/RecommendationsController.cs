using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieMind.Application.Dtos.Recommendations;
using MovieMind.Application.Interfaces;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize]
public sealed class RecommendationsController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<RecommendationResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecommendationResultDto>> Get(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 50)
        {
            return BadRequest(new { message = "Count must be between 1 and 50." });
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Unauthorized();
        }

        var result = await recommendationService.GetForUserAsync(userId, count, cancellationToken);
        return result is null
            ? NotFound(new { message = "The authenticated user was not found." })
            : Ok(result);
    }
}