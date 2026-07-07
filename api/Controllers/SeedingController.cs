using api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeedingController : ControllerBase
{
    private readonly ISeedingService _seedingService;
    private readonly ILogger<SeedingController> _logger;
    private readonly IWebHostEnvironment _environment;

    public SeedingController(
        ISeedingService seedingService,
        ILogger<SeedingController> logger,
        IWebHostEnvironment environment)
    {
        _seedingService = seedingService;
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("moonboard")]
    public async Task<ActionResult> SeedMoonboardData()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var (locations, setters, routes) = await _seedingService.SeedMoonboardDataAsync();

            return Ok(new
            {
                message = "Successfully seeded all Moonboard data",
                locations,
                setters,
                routes,
                total = locations + setters + routes
            });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Seeding file not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid seeding data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Moonboard data");
            return StatusCode(500, "An error occurred while seeding Moonboard data");
        }
    }

    [HttpPost("locations")]
    public async Task<ActionResult> SeedMBLocations()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var count = await _seedingService.SeedMBLocationsAsync();
            return Ok(new { message = $"Successfully seeded {count} locations", count });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Locations file not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid locations data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding locations");
            return StatusCode(500, "An error occurred while seeding locations");
        }
    }

    [HttpPost("setters")]
    public async Task<ActionResult> SeedMBSetters()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var count = await _seedingService.SeedMBSettersAsync();
            return Ok(new { message = $"Successfully seeded {count} setters", count });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Setters file not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid setters data");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding setters");
            return StatusCode(500, "An error occurred while seeding setters");
        }
    }
}
