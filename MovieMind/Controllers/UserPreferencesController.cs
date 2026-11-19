using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieMind.Application.Dtos.Preferences;
using MovieMind.Application.Interfaces;

namespace MovieMind.API.Controllers;

[ApiController]
[Route("api/users/me/preferences")]
[Authorize]
public sealed class UserPreferencesController(IUserPreferencesService preferencesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var preferences = await preferencesService.GetAsync(userId.Value, cancellationToken);
        return preferences is null
            ? NotFound(new { message = "Utilizatorul nu a fost găsit." })
            : Ok(preferences);
    }

    [HttpGet("vector")]
    public async Task<IActionResult> GetVector(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var preferences = await preferencesService.GetAsync(userId.Value, cancellationToken);
        return preferences is null
            ? NotFound(new { message = "Utilizatorul nu a fost găsit." })
            : Ok(preferences.Vector);
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] UpdateUserPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var preferences = await preferencesService.UpdateAsync(userId.Value, request, cancellationToken);
            return preferences is null
                ? NotFound(new { message = "Utilizatorul sau unul dintre genuri nu a fost găsit." })
                : Ok(preferences);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private Guid? GetCurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
}