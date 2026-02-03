using api.Models;

namespace api.Interfaces;

public interface ILocationService
{
    Task<IEnumerable<Location>> GetLocationsAsync();
    Task<Location?> GetLocationByIdAsync(int id);
    Task<Location?> GetLocationByNameAsync(string name);
    Task<Location> CreateLocationAsync(Location location);
    Task<Location?> UpdateLocationAsync(int id, Location location);
    Task<bool> DeleteLocationAsync(int id);
    Task<bool> LocationExistsAsync(int id);
}
