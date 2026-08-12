using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ISocialService _socialService;

    public FeedController(IUserService userService, ISocialService socialService)
    {
        _userService = userService;
        _socialService = socialService;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResult<LogEntryDto>>> GetFeed(
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        return Ok(await _socialService.GetFeedAsync(
            caller.Id, skip, take ?? DefaultPageSize, cancellationToken));
    }
}
