namespace api.Interfaces;

public interface ISeedingService
{
    Task<int> SeedMBLocationsAsync();
    Task<int> SeedMBSettersAsync();
    Task<int> SeedMBRoutesAsync();
    Task<(int locations, int setters, int routes)> SeedMoonboardDataAsync();
}
