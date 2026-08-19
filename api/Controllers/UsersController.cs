using api.DTO;
using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ICatalogService _catalogService;
    private readonly IFollowService _followService;
    private readonly ISocialService _socialService;

    public UsersController(
        IUserService userService,
        ICatalogService catalogService,
        IFollowService followService,
        ISocialService socialService)
    {
        _userService = userService;
        _catalogService = catalogService;
        _followService = followService;
        _socialService = socialService;
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

    [Authorize]
    [HttpGet("me/stats")]
    public async Task<ActionResult<HomeStatsDto>> GetMyStats(CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);
        return Ok(await _catalogService.GetHomeStatsAsync(user.Id, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> SearchUsers(
        [FromQuery] string q = "",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var caller = await TryGetCallerAsync(_userService, cancellationToken);
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

        var caller = await TryGetCallerAsync(_userService, cancellationToken);
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

        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        var page = await _catalogService.GetLogEntriesPagedAsync(
            user.Id, skip, take ?? DefaultPageSize, cancellationToken);
        await _socialService.DecorateLogEntriesAsync(page.Items, caller?.Id, cancellationToken);
        return Ok(page);
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

        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        return Ok(await query(user.Id, caller?.Id, skip, take ?? DefaultPageSize, cancellationToken));
    }
}
