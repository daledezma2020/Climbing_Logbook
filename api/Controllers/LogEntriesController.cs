using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogEntriesController : ApiControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly IUserService _userService;
    private readonly ISocialService _socialService;

    public LogEntriesController(
        ICatalogService catalogService,
        IUserService userService,
        ISocialService socialService)
    {
        _catalogService = catalogService;
        _userService = userService;
        _socialService = socialService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogEntryDto>>> GetLogEntries(
        [FromQuery] int? userId,
        CancellationToken cancellationToken)
    {
        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        var entries = await _catalogService.GetLogEntriesAsync(userId);
        await _socialService.DecorateLogEntriesAsync(entries, caller?.Id, cancellationToken);
        return Ok(entries);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LogEntryDto>> GetLogEntry(int id, CancellationToken cancellationToken)
    {
        var entry = await _catalogService.GetLogEntryAsync(id, cancellationToken);
        if (entry is null)
        {
            return NotFound();
        }

        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        await _socialService.DecorateLogEntriesAsync([entry], caller?.Id, cancellationToken);
        return Ok(entry);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<LogEntryDto>> CreateLogEntry(CreateLogEntryDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);
        var entry = await _catalogService.CreateLogEntryAsync(dto, user.Id);
        return CreatedAtAction(nameof(GetLogEntry), new { id = entry.Id }, entry);
    }

    [Authorize]
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> Like(int id, CancellationToken cancellationToken)
    {
        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        return await _socialService.LikeAsync(caller.Id, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [Authorize]
    [HttpDelete("{id:int}/like")]
    public async Task<IActionResult> Unlike(int id, CancellationToken cancellationToken)
    {
        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        return await _socialService.UnlikeAsync(caller.Id, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(
        int id,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        return Ok(await _socialService.GetLogEntryCommentsAsync(
            id, caller?.Id, skip, take ?? DefaultPageSize, cancellationToken));
    }

    [Authorize]
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        int id,
        CreateCommentDto dto,
        CancellationToken cancellationToken)
    {
        var caller = await _userService.EnsureUserAsync(User, cancellationToken);
        var comment = await _socialService.AddLogEntryCommentAsync(caller.Id, id, dto, cancellationToken);
        if (comment is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetComments), new { id }, comment);
    }
}
