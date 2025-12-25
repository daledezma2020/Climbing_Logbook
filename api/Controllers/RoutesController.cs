using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;

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
    [HttpPost]
    public async Task<ActionResult<ClimbRoute>> CreateRoute([FromBody] ClimbRoute climbRoute)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
    [HttpPut("{id}")]
    public async Task<ActionResult<ClimbRoute>> UpdateRoute(int id, [FromBody] ClimbRoute climbRoute)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
}
