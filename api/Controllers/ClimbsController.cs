using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClimbsController : ApiControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly IUserService _userService;
    private readonly ISocialService _socialService;

    public ClimbsController(
        ICatalogService catalogService,
        IUserService userService,
        ISocialService socialService)
    {
        _catalogService = catalogService;
        _userService = userService;
        _socialService = socialService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClimbSummaryDto>>> GetClimbs()
    {
        return Ok(await _catalogService.GetClimbsAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClimbSummaryDto>> GetClimb(int id)
    {
        var climb = await _catalogService.GetClimbAsync(id);
        return climb == null ? NotFound() : Ok(climb);
    }

    [HttpGet("{id:int}/logentries")]
    public async Task<ActionResult<PagedResult<LogEntryDto>>> GetClimbLogEntries(
        int id,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        if (await _catalogService.GetClimbAsync(id) is null)
        {
            return NotFound();
        }

        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        var page = await _catalogService.GetClimbLogEntriesPagedAsync(
            id, skip, take ?? DefaultPageSize, cancellationToken);
        await _socialService.DecorateLogEntriesAsync(page.Items, caller?.Id, cancellationToken);
        return Ok(page);
    }

    [HttpGet("{id:int}/comments")]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments(
        int id,
        [FromQuery] int skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var caller = await TryGetCallerAsync(_userService, cancellationToken);
        return Ok(await _socialService.GetClimbCommentsAsync(
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
        var comment = await _socialService.AddClimbCommentAsync(caller.Id, id, dto, cancellationToken);
        if (comment is null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetComments), new { id }, comment);
    }

    [Authorize]
    [HttpPost("manual")]
    public async Task<ActionResult<ClimbSummaryDto>> CreateManualClimb(CreateManualClimbDto dto)
    {
        var climb = await _catalogService.CreateManualClimbAsync(dto);
        return CreatedAtAction(nameof(GetClimb), new { id = climb.Id }, climb);
    }

    [Authorize]
    [HttpPost("openbeta/{uuid}/import")]
    public async Task<ActionResult<ClimbSummaryDto>> ImportOpenBetaClimb(string uuid)
    {
        var climb = await _catalogService.ImportOpenBetaClimbAsync(uuid);
        return climb == null ? BadRequest("OpenBeta climb could not be imported.") : Ok(climb);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteClimb(int id)
    {
        return await _catalogService.DeleteClimbAsync(id) ? NoContent() : NotFound();
    }
}
