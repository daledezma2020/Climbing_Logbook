using api.DTO;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacesController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public PlacesController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlaceSummaryDto>>> GetPlaces()
    {
        return Ok(await _catalogService.GetPlacesAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<PlaceSummaryDto>> CreatePlace(CreatePlaceDto dto)
    {
        var place = await _catalogService.CreatePlaceAsync(dto);
        return CreatedAtAction(nameof(GetPlaces), new { id = place.Id }, place);
    }

    [Authorize]
    [HttpPost("osm/{osmType}/{osmId}/import")]
    public async Task<ActionResult<PlaceSummaryDto>> ImportOsmPlace(string osmType, string osmId)
    {
        var place = await _catalogService.ImportOsmPlaceAsync(osmType, osmId);
        return place == null ? BadRequest("OSM place could not be imported.") : Ok(place);
    }
}
