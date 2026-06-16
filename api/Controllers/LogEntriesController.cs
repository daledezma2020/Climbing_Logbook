using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogEntriesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public LogEntriesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LogEntryDto>>> GetLogEntries()
    {
        return Ok(await _catalogService.GetLogEntriesAsync());
    }

    [HttpPost]
    public async Task<ActionResult<LogEntryDto>> CreateLogEntry(CreateLogEntryDto dto)
    {
        var entry = await _catalogService.CreateLogEntryAsync(dto);
        return CreatedAtAction(nameof(GetLogEntries), new { id = entry.Id }, entry);
    }
}
