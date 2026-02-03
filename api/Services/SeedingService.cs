using api.Interfaces;
using api.Models;
using System.Text.Json;

namespace api.Services;

public class SeedingService : ISeedingService
{
    private readonly IClimbRouteService _climbRouteService;
    private readonly ILocationService _locationService;
    private readonly ISetterService _setterService;

    public SeedingService(
        IClimbRouteService climbRouteService,
        ILocationService locationService,
        ISetterService setterService)
    {
        _climbRouteService = climbRouteService;
        _locationService = locationService;
        _setterService = setterService;
    }

    public async Task<int> SeedMBLocationsAsync()
    {
        var locationsPath = Path.Combine(Directory.GetCurrentDirectory(), "seeding", "locations.json");

        if (!File.Exists(locationsPath))
        {
            throw new FileNotFoundException("locations.json file not found", locationsPath);
        }

        var jsonContent = await File.ReadAllTextAsync(locationsPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var locationsArray = jsonDoc.RootElement;

        if (locationsArray.ValueKind != JsonValueKind.Array || locationsArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("No locations found in the file");
        }

        var createdCount = 0;
        foreach (var locationElement in locationsArray.EnumerateArray())
        {
            var name = locationElement.GetProperty("name").GetString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var location = new Location { Name = name };
                await _locationService.CreateLocationAsync(location);
                createdCount++;
            }
        }

        return createdCount;
    }

    public async Task<int> SeedMBSettersAsync()
    {
        var settersPath = Path.Combine(Directory.GetCurrentDirectory(), "seeding", "setters.json");

        if (!File.Exists(settersPath))
        {
            throw new FileNotFoundException("setters.json file not found", settersPath);
        }

        var jsonContent = await File.ReadAllTextAsync(settersPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var settersArray = jsonDoc.RootElement;

        if (settersArray.ValueKind != JsonValueKind.Array || settersArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("No setters found in the file");
        }

        var createdCount = 0;
        foreach (var setterElement in settersArray.EnumerateArray())
        {
            var name = setterElement.GetProperty("name").GetString();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var setter = new Setter { Name = name };
                await _setterService.CreateSetterAsync(setter);
                createdCount++;
            }
        }

        return createdCount;
    }

    public async Task<int> SeedMBRoutesAsync()
    {   
        var benchmarksPath = Path.Combine(Directory.GetCurrentDirectory(), "seeding", "benchmarks.json");

        if (!File.Exists(benchmarksPath))
        {
            throw new FileNotFoundException("benchmarks.json file not found", benchmarksPath);
        }

        var jsonContent = await File.ReadAllTextAsync(benchmarksPath);
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var benchmarksArray = jsonDoc.RootElement;

        if (benchmarksArray.ValueKind != JsonValueKind.Array || benchmarksArray.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("No benchmarks found in the file");
        }

        var moonboardRoutes = new List<ClimbRoute>();

        foreach (var benchmark in benchmarksArray.EnumerateArray())
        {
            var name = benchmark.GetProperty("name").GetString() ?? "Unknown";
            var grade = benchmark.GetProperty("grade").GetInt32();
            var mbType = benchmark.GetProperty("mb_type").GetInt32();
            var setterName = benchmark.GetProperty("setter").GetString();

            // Map mb_type to location name
            var locationName = MapMoonboardType(mbType);
            var location = await _locationService.GetLocationByNameAsync(locationName);

            // Get setter by name
            Setter? setter = null;
            if (!string.IsNullOrWhiteSpace(setterName))
            {
                setter = await _setterService.GetSetterByNameAsync(setterName);
            }

            var route = new ClimbRoute
            {
                Name = name,
                Grade = $"V{grade}",
                LocationId = location?.Id ?? 1,
                SetterId = setter?.Id ?? 1,
                Type = "Board",
                AverageRating = 5
            };

            moonboardRoutes.Add(route);
        }

        return await _climbRouteService.SeedMoonboardRoutesAsync(moonboardRoutes);
    }

    public async Task<(int locations, int setters, int routes)> SeedMoonboardDataAsync()
    {
        var locationsCount = await SeedMBLocationsAsync();
        var settersCount = await SeedMBSettersAsync();
        var routesCount = await SeedMBRoutesAsync();

        return (locationsCount, settersCount, routesCount);
    }

    private static string MapMoonboardType(int mbType)
    {
        return mbType switch
        {
            0 => "Moonboard 2016",
            1 => "Moonboard 2017",
            2 => "Moonboard 2019",
            3 => "Mini Moonboard 2020",
            4 => "Moonboard 2024",
            5 => "Mini Moonboard 2025",
            _ => "Unknown Moonboard" // Default fallback
        };
    }
}
