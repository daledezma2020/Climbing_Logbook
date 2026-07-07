using api.DTO.ClimbRoute;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly IClimbRouteService _climbRouteService;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(IClimbRouteService climbRouteService, ILogger<RoutesController> logger)
    {
        _climbRouteService = climbRouteService;
        _logger = logger;
    }

    // GET: api/routes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClimbRoute>>> GetRoutes()
    {
        try
        {
            var routes = await _climbRouteService.GetAllClimbRoutesAsync();
            return Ok(routes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving routes");
            return StatusCode(500, "An error occurred while retrieving routes");
        }
    }

    // GET: api/routes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ClimbRoute>> GetRoute(int id)
    {
        try
        {
            var route = await _climbRouteService.GetClimbRouteByIdAsync(id);

            if (route == null)
            {
                return NotFound($"Route with ID {id} not found");
            }

            return Ok(route);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving route with ID {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the route");
        }
    }

    // POST: api/routes
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ClimbRoute>> CreateRoute([FromBody] ClimbRouteDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var climbRoute = new ClimbRoute
            {
                Name = dto.Name,
                Grade = dto.Grade,
                AverageRating = dto.AverageRating,
                Picture = dto.Picture,
                Video = dto.Video,
                Location = dto.Location
            };

            var createdRoute = await _climbRouteService.CreateClimbRouteAsync(climbRoute);
            return CreatedAtAction(nameof(GetRoute), new { id = createdRoute.Id }, createdRoute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating route");
            return StatusCode(500, "An error occurred while creating the route");
        }
    }

    // PUT: api/routes/5
    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ClimbRoute>> UpdateRoute(int id, [FromBody] ClimbRouteDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var climbRoute = new ClimbRoute
            {
                Name = dto.Name,
                Grade = dto.Grade,
                AverageRating = dto.AverageRating,
                Picture = dto.Picture,
                Video = dto.Video,
                Location = dto.Location
            };

            var updatedRoute = await _climbRouteService.UpdateClimbRouteAsync(id, climbRoute);

            if (updatedRoute == null)
            {
                return NotFound($"Route with ID {id} not found");
            }

            return Ok(updatedRoute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating route with ID {Id}", id);
            return StatusCode(500, "An error occurred while updating the route");
        }
    }

    // DELETE: api/routes/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        try
        {
            var result = await _climbRouteService.DeleteClimbRouteAsync(id);

            if (!result)
            {
                return NotFound($"Route with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting route with ID {Id}", id);
            return StatusCode(500, "An error occurred while deleting the route");
        }
    }

    // POST: api/routes/seed-moonboard
    [Authorize]
    [HttpPost("seed-moonboard")]
    public async Task<ActionResult> SeedMoonboardRoutes()
    {
        try
        {
            var benchmarksPath = Path.Combine(Directory.GetCurrentDirectory(), "seeding", "benchmarks.json");

            if (!System.IO.File.Exists(benchmarksPath))
            {
                return NotFound("benchmarks.json file not found");
            }

            var jsonContent = await System.IO.File.ReadAllTextAsync(benchmarksPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var benchmarks = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent, options);

            if (benchmarks == null || !benchmarks.Any())
            {
                return BadRequest("No benchmarks found in the file");
            }

            var moonboardRoutes = benchmarks.Select(benchmark => new ClimbRoute
            {
                Name = benchmark.GetProperty("name").GetString() ?? "Unknown",
                Grade = $"V{benchmark.GetProperty("grade").GetInt32()}",
                Location = MapMoonboardType(benchmark.GetProperty("mb_type").GetInt32()),
                Setter = benchmark.GetProperty("setter").GetString(),
                Type = "Board",
                AverageRating = 5
            }).ToList();

            var count = await _climbRouteService.SeedMoonboardRoutesAsync(moonboardRoutes);

            return Ok(new { message = $"Successfully seeded {count} Moonboard routes", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding Moonboard routes");
            return StatusCode(500, "An error occurred while seeding Moonboard routes");
        }
    }

    // DELETE: api/routes/delete-all
    // WARNING: This endpoint is for development/testing purposes only
    [Authorize]
    [HttpDelete("delete-all")]
    public async Task<ActionResult> DeleteAllRoutes()
    {
        try
        {
            var count = await _climbRouteService.DeleteAllClimbRoutesAsync();

            return Ok(new { message = $"Successfully deleted {count} routes from the database", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all routes");
            return StatusCode(500, "An error occurred while deleting all routes");
        }
    }

    private static string MapMoonboardType(int mbType)
    {
        return mbType switch
        {
            0 => "MoonBoard 2016",
            1 => "MoonBoard 2017",
            2 => "MoonBoard 2019",
            3 => "Mini MoonBoard 2020",
            4 => "MoonBoard 2024",
            5 => "Mini MoonBoard 2025",
            _ => $"MoonBoard (Unknown Type {mbType})"
        };
    }
}
