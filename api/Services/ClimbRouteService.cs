using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class ClimbRouteService : IClimbRouteService
{
    private readonly ApplicationDbContext _context;

    public ClimbRouteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClimbRoute>> GetAllClimbRoutesAsync()
    {
        return await _context.ClimbRoutes
            .Include(r => r.Comments)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<ClimbRoute?> GetClimbRouteByIdAsync(int id)
    {
        return await _context.ClimbRoutes
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ClimbRoute> CreateClimbRouteAsync(ClimbRoute climbRoute)
    {
        climbRoute.CreatedAt = DateTime.UtcNow;
        _context.ClimbRoutes.Add(climbRoute);
        await _context.SaveChangesAsync();
        return climbRoute;
    }

    public async Task<ClimbRoute?> UpdateClimbRouteAsync(int id, ClimbRoute climbRoute)
    {
        var existingClimbRoute = await _context.ClimbRoutes.FindAsync(id);

        if (existingClimbRoute == null)
        {
            return null;
        }

        existingClimbRoute.Grade = climbRoute.Grade;
        existingClimbRoute.AverageRating = climbRoute.AverageRating;
        existingClimbRoute.Picture = climbRoute.Picture;
        existingClimbRoute.Video = climbRoute.Video;
        existingClimbRoute.Location = climbRoute.Location;
        existingClimbRoute.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ClimbRouteExistsAsync(id))
            {
                return null;
            }
            throw;
        }

        return existingClimbRoute;
    }

    public async Task<bool> DeleteClimbRouteAsync(int id)
    {
        var climbRoute = await _context.ClimbRoutes.FindAsync(id);

        if (climbRoute == null)
        {
            return false;
        }

        _context.ClimbRoutes.Remove(climbRoute);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClimbRouteExistsAsync(int id)
    {
        return await _context.ClimbRoutes.AnyAsync(r => r.Id == id);
    }
}
