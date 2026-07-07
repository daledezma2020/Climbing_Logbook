using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public SearchController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResponseDto>> Search(
        [FromQuery] string q = "",
        [FromQuery] string? bbox = null,
        [FromQuery] int limit = 10)
    {
        return Ok(await _catalogService.SearchAsync(q, bbox, limit));
    }
}
