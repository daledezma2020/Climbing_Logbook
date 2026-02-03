using api.DTO.ClimbRoute;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
    {
        try
        {
            var locations = await _locationService.GetLocationsAsync();
            return Ok(locations);
        }
        catch
        {
            _logger.LogError("An error occurred while getting locations.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Location>> GetLocation(int id)
    {
        try
        {
            var location = await _locationService.GetLocationByIdAsync(id);
            if (location == null){
                return NotFound();
            }
            return Ok(location);
        }
        catch
        {
            _logger.LogError($"An error occurred while getting location with id {id}.");
            return StatusCode(500, "Internal server error");
        }
    }
}