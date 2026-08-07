using api.DTO;
using api.Interfaces;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private const int DefaultPageSize = 25;

    private readonly IUserService _userService;
    private readonly ICatalogService _catalogService;

    public UsersController(IUserService userService, ICatalogService catalogService)
    {
        _userService = userService;
        _catalogService = catalogService;
    }

    // The literal "me" routes are declared first, and "me" is a reserved username,
    // so they can never be shadowed by the {username} routes below.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);
        return Ok(await _userService.GetProfileAsync(user, includePrivate: true, cancellationToken));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);

        try
        {
            var updated = await _userService.UpdateProfileAsync(user.Id, dto, cancellationToken);
            return Ok(await _userService.GetProfileAsync(updated, includePrivate: true, cancellationToken));
        }
        catch (UsernameConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Username unavailable",
                Detail = ex.Message
            });
        }
        catch (ProfileValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Profile could not be saved",
                Detail = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Avatar could not be uploaded",
                Detail = "Attach an image file in the \"file\" field."
            });
        }

        var user = await _userService.EnsureUserAsync(User, cancellationToken);

        try
        {
            var updated = await _userService.SetAvatarAsync(user.Id, file, cancellationToken);
            return Ok(await _userService.GetProfileAsync(updated, includePrivate: true, cancellationToken));
        }
        catch (ProfileValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Avatar could not be uploaded",
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string username, CancellationToken cancellationToken)
    {
        var user = await _userService.FindByUsernameAsync(username, cancellationToken);
        return user is null
            ? NotFound()
            : Ok(await _userService.GetProfileAsync(user, includePrivate: false, cancellationToken));
    }

    [HttpGet("{username}/logentries")]
    public async Task<ActionResult<PagedResult<LogEntryDto>>> GetProfileLogEntries(
        string username,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var user = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(await _catalogService.GetLogEntriesPagedAsync(
            user.Id, skip, take ?? DefaultPageSize, cancellationToken));
    }
}
