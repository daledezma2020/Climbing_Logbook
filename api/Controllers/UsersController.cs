using api.DTO;
using api.Interfaces;
using api.Models;
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
    private readonly IFollowService _followService;

    public UsersController(
        IUserService userService,
        ICatalogService catalogService,
        IFollowService followService)
    {
        _userService = userService;
        _catalogService = catalogService;
        _followService = followService;
    }

    // The literal "me" routes are declared first, and "me" is a reserved username,
    // so they can never be shadowed by the {username} routes below.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);
        return Ok(await _userService.GetProfileAsync(user, user, cancellationToken));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateUserProfileDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);

        try
        {
            var updated = await _userService.UpdateProfileAsync(user.Id, dto, cancellationToken);
            return Ok(await _userService.GetProfileAsync(updated, updated, cancellationToken));
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
            return Ok(await _userService.GetProfileAsync(updated, updated, cancellationToken));
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

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> SearchUsers(
        [FromQuery] string q = "",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var caller = await TryGetCallerAsync(cancellationToken);
        return Ok(await _userService.SearchUsersAsync(q, limit, caller?.Id, cancellationToken));
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(string username, CancellationToken cancellationToken)
    {
        var user = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var caller = await TryGetCallerAsync(cancellationToken);
        return Ok(await _userService.GetProfileAsync(user, caller, cancellationToken));
    }

    [Authorize]
    [HttpPost("{username}/follow")]
    public async Task<IActionResult> Follow(string username, CancellationToken cancellationToken)
    {
        var target = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        var caller = await _userService.EnsureUserAsync(User, cancellationToken);

        try
        {
            await _followService.FollowAsync(caller.Id, target.Id, cancellationToken);
        }
        catch (SelfFollowException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Follow could not be saved",
                Detail = ex.Message
            });
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{username}/follow")]
    public async Task<IActionResult> Unfollow(string username, CancellationToken cancellationToken)
    {
        var target = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        await _followService.UnfollowAsync(caller.Id, target.Id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{username}/followers")]
    public Task<ActionResult<PagedResult<UserSummaryDto>>> GetFollowers(
        string username,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        return GetConnectionsAsync(username, skip, take, _followService.GetFollowersAsync, cancellationToken);
    }

    [HttpGet("{username}/following")]
    public Task<ActionResult<PagedResult<UserSummaryDto>>> GetFollowing(
        string username,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        return GetConnectionsAsync(username, skip, take, _followService.GetFollowingAsync, cancellationToken);
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

    private delegate Task<PagedResult<UserSummaryDto>> ConnectionPageQuery(
        int userId,
        int? callerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    private async Task<ActionResult<PagedResult<UserSummaryDto>>> GetConnectionsAsync(
        string username,
        int skip,
        int? take,
        ConnectionPageQuery query,
        CancellationToken cancellationToken)
    {
        var user = await _userService.FindByUsernameAsync(username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var caller = await TryGetCallerAsync(cancellationToken);
        return Ok(await query(user.Id, caller?.Id, skip, take ?? DefaultPageSize, cancellationToken));
    }

    // These endpoints serve visitors and signed-in users alike. EnsureUserAsync throws
    // without a subject claim, so it is only reached once the request is authenticated.
    private async Task<AppUser?> TryGetCallerAsync(CancellationToken cancellationToken)
    {
        return User.Identity?.IsAuthenticated == true
            ? await _userService.EnsureUserAsync(User, cancellationToken)
            : null;
    }
}
