using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettersController : ControllerBase
{
    private readonly ISetterService _setterService;
    private readonly ILogger<SettersController> _logger;

    public SettersController(ISetterService setterService, ILogger<SettersController> logger)
    {
        _setterService = setterService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Setter>>> GetSetters()
    {
        try
        {
            var setters = await _setterService.GetSettersAsync();
            return Ok(setters);
        }
        catch
        {
            _logger.LogError("An error occurred while getting setters.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Setter>> GetSetter(int id)
    {
        try
        {
            var setter = await _setterService.GetSetterByIdAsync(id);
            if (setter == null){
                return NotFound();
            }
            return Ok(setter);
        }
        catch
        {
            _logger.LogError($"An error occurred while getting setter with id {id}.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Setter>> CreateSetter(Setter setter)
    {
        try
        {
            var createdSetter = await _setterService.CreateSetterAsync(setter);
            return CreatedAtAction(nameof(GetSetter), new { id = createdSetter.Id }, createdSetter);
        }
        catch
        {
            _logger.LogError("An error occurred while creating a new setter.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Setter>> UpdateSetter(int id, Setter setter)
    {
        try
        {
            var updatedSetter = await _setterService.UpdateSetterAsync(id, setter);
            if (updatedSetter == null)
            {
                return NotFound();
            }
            return Ok(updatedSetter);
        }
        catch
        {
            _logger.LogError($"An error occurred while updating setter with id {id}.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSetter(int id)
    {
        try
        {
            var deleted = await _setterService.DeleteSetterAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch
        {
            _logger.LogError($"An error occurred while deleting setter with id {id}.");
            return StatusCode(500, "Internal server error");
        }
    }
}