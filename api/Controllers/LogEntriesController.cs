using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogEntriesController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly IUserService _userService;

    public LogEntriesController(ICatalogService catalogService, IUserService userService)
    {
        _catalogService = catalogService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogEntryDto>>> GetLogEntries([FromQuery] int? userId)
    {
        return Ok(await _catalogService.GetLogEntriesAsync(userId));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<LogEntryDto>> CreateLogEntry(CreateLogEntryDto dto, CancellationToken cancellationToken)
    {
        var user = await _userService.EnsureUserAsync(User, cancellationToken);
        var entry = await _catalogService.CreateLogEntryAsync(dto, user.Id);
        return CreatedAtAction(nameof(GetLogEntries), new { id = entry.Id }, entry);
    }
}
