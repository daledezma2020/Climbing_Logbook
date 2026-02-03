using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;

    public LocationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Location>> GetLocationsAsync()
    {
        return await _context.Locations
            .OrderByDescending(s => s.Name)
            .ToListAsync();
    }

    public async Task<Location?> GetLocationByIdAsync(int id)
    {
        return await _context.Locations
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        return await _context.Locations
            .FirstOrDefaultAsync(l => l.Name == name);
    }

    public async Task<Location> CreateLocationAsync(Location location)
    {
        _context.Locations.Add(location);
        await _context.SaveChangesAsync();
        return location;
    }

    public async Task<Location?> UpdateLocationAsync(int id, Location location)
    {
        var existingLocation = await _context.Locations.FindAsync(id);

        if (existingLocation == null)
        {
            return null;
        }

        existingLocation.Name = location.Name;
        existingLocation.Latitude = location.Latitude;
        existingLocation.Longitude = location.Longitude;
        existingLocation.Address = location.Address;
        existingLocation.City = location.City;
        existingLocation.State = location.State;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await LocationExistsAsync(id))
            {
                return null;
            }
            throw;
        }

        return existingLocation;
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        var location = await _context.Locations.FindAsync(id);

        if (location == null)
        {
            return false;
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LocationExistsAsync(int id)
    {
        return await _context.Locations.AnyAsync(s => s.Id == id);
    }
}
