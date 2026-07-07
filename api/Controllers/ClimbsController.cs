using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClimbsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public ClimbsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
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
