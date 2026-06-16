using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoardConfigurationsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public BoardConfigurationsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BoardConfigurationDto>>> GetBoardConfigurations()
    {
        return Ok(await _catalogService.GetBoardConfigurationsAsync());
    }
}
