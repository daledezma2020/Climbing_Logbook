using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class CatalogService : ICatalogService
{
    private readonly ApplicationDbContext _context;
    private readonly IOpenBetaClient _openBetaClient;
    private readonly IOsmClient _osmClient;
    private readonly ISetterService _setterService;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(
        ApplicationDbContext context,
        IOpenBetaClient openBetaClient,
        IOsmClient osmClient,
        ISetterService setterService,
        ILogger<CatalogService> logger)
    {
        _context = context;
        _openBetaClient = openBetaClient;
        _osmClient = osmClient;
        _setterService = setterService;
        _logger = logger;
    }

    public async Task<List<ClimbSummaryDto>> GetClimbsAsync()
    {
        var climbs = await IncludeClimbSummary(_context.Climbs)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return climbs.Select(CatalogMapping.ToDto).ToList();
    }

    public async Task<ClimbSummaryDto?> GetClimbAsync(int id)
    {
        var climb = await IncludeClimbSummary(_context.Climbs)
            .FirstOrDefaultAsync(c => c.Id == id);
        return climb == null ? null : CatalogMapping.ToDto(climb);
    }

    public async Task<ClimbSummaryDto> CreateManualClimbAsync(CreateManualClimbDto dto)
    {
        var hasBoard = dto.BoardConfigurationId.HasValue
            || !string.IsNullOrWhiteSpace(dto.BoardConfigurationName);
        ValidateClimbContext(dto.PlaceId, hasBoard, dto.CustomLocation);

        Place? place = null;
        if (dto.PlaceId.HasValue)
        {
            place = await _context.Places.FindAsync(dto.PlaceId.Value)
                ?? throw new InvalidOperationException("Place not found.");
        }

        var boardConfiguration = await ResolveBoardConfigurationAsync(
            dto.BoardConfigurationId, dto.BoardConfigurationName);
        var setter = await ResolveSetterAsync(dto.SetterId, dto.SetterName);

        var climb = new Climb
        {
            Name = dto.Name.Trim(),
            Discipline = dto.Discipline,
            GradeSystem = dto.GradeSystem,
            Grade = dto.Grade.Trim(),
            Place = place,
            PlaceId = place == null ? dto.PlaceId : null,
            BoardConfiguration = boardConfiguration,
            BoardConfigurationId = boardConfiguration is { Id: > 0 } ? boardConfiguration.Id : null,
            CustomLocationName = dto.CustomLocation?.Name.Trim(),
            CustomLocationLatitude = dto.CustomLocation?.Latitude,
            CustomLocationLongitude = dto.CustomLocation?.Longitude,
            Setter = setter,
            SetterId = setter is { Id: > 0 } ? setter.Id : null,
            FirstAscentName = dto.FirstAscentName,
            PictureUrl = dto.PictureUrl,
            VideoUrl = dto.VideoUrl
        };

        _context.Climbs.Add(climb);
        await _context.SaveChangesAsync();

        var saved = await IncludeClimbSummary(_context.Climbs).FirstAsync(c => c.Id == climb.Id);
        return CatalogMapping.ToDto(saved);
    }

    public async Task<ClimbSummaryDto?> ImportOpenBetaClimbAsync(string uuid)
    {
        var existingReference = await _context.ClimbExternalReferences
            .Include(r => r.Climb)
            .FirstOrDefaultAsync(r => r.Provider == ExternalProvider.OpenBeta && r.ExternalId == uuid);
        if (existingReference?.Climb != null)
        {
            return await GetClimbAsync(existingReference.ClimbId);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var details = await _openBetaClient.GetClimbAsync(uuid, timeout.Token);
        if (details == null || string.IsNullOrWhiteSpace(details.Grade))
        {
            return null;
        }

        Place? place = null;
        if (!string.IsNullOrWhiteSpace(details.ParentAreaUuid))
        {
            var placeReference = await _context.PlaceExternalReferences
                .Include(r => r.Place)
                .FirstOrDefaultAsync(r => r.Provider == ExternalProvider.OpenBeta && r.ExternalId == details.ParentAreaUuid);
            place = placeReference?.Place;
        }

        if (place == null)
        {
            place = await FindNearbyPlaceAsync(details.ParentAreaName ?? "OpenBeta area", PlaceKind.Outdoor, details.Latitude, details.Longitude)
                ?? new Place
                {
                    Name = details.ParentAreaName ?? "OpenBeta area",
                    Kind = PlaceKind.Outdoor,
                    Latitude = details.Latitude,
                    Longitude = details.Longitude
                };
            _context.Places.Add(place);
        }

        var climb = new Climb
        {
            Name = details.Name,
            Discipline = ClimbDiscipline.Bouldering,
            GradeSystem = GradeSystem.VScale,
            Grade = details.Grade,
            FirstAscentName = details.FirstAscentName,
            Place = place
        };

        _context.Climbs.Add(climb);
        _context.ClimbExternalReferences.Add(new ClimbExternalReference
        {
            Climb = climb,
            Provider = ExternalProvider.OpenBeta,
            ExternalId = details.Uuid,
            ExternalUrl = $"https://openbeta.io/climb/{details.Uuid}"
        });

        if (!string.IsNullOrWhiteSpace(details.ParentAreaUuid)
            && !await _context.PlaceExternalReferences.AnyAsync(r => r.Provider == ExternalProvider.OpenBeta && r.ExternalId == details.ParentAreaUuid))
        {
            _context.PlaceExternalReferences.Add(new PlaceExternalReference
            {
                Place = place,
                Provider = ExternalProvider.OpenBeta,
                ExternalId = details.ParentAreaUuid,
                ExternalUrl = $"https://openbeta.io/area/{details.ParentAreaUuid}"
            });
        }

        await _context.SaveChangesAsync();
        var saved = await IncludeClimbSummary(_context.Climbs).FirstAsync(c => c.Id == climb.Id);
        return CatalogMapping.ToDto(saved);
    }

    public async Task<bool> DeleteClimbAsync(int id)
    {
        var climb = await _context.Climbs.FindAsync(id);
        if (climb == null)
        {
            return false;
        }

        _context.Climbs.Remove(climb);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PlaceSummaryDto>> GetPlacesAsync()
    {
        var places = await _context.Places.OrderBy(p => p.Name).ToListAsync();
        return places.Select(CatalogMapping.ToDto).ToList();
    }

    public async Task<PlaceSummaryDto> CreatePlaceAsync(CreatePlaceDto dto)
    {
        var place = CreatePlaceEntity(dto);
        _context.Places.Add(place);
        await _context.SaveChangesAsync();
        return CatalogMapping.ToDto(place);
    }

    public async Task<PlaceSummaryDto?> ImportOsmPlaceAsync(string osmType, string osmId)
    {
        var externalId = $"{osmType}/{osmId}";
        var existingReference = await _context.PlaceExternalReferences
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.Provider == ExternalProvider.OpenStreetMap && r.ExternalId == externalId);
        if (existingReference?.Place != null)
        {
            return CatalogMapping.ToDto(existingReference.Place);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var details = await _osmClient.GetPlaceAsync(osmType, osmId, timeout.Token);
        if (details == null)
        {
            return null;
        }

        var kind = details.Name.Contains("gym", StringComparison.OrdinalIgnoreCase) ? PlaceKind.Gym : PlaceKind.Outdoor;
        var place = await FindNearbyPlaceAsync(details.Name, kind, details.Latitude, details.Longitude)
            ?? new Place
            {
                Name = details.Name,
                Kind = kind,
                Latitude = details.Latitude,
                Longitude = details.Longitude,
                Address = details.Address,
                City = details.City,
                State = details.State,
                Country = details.Country
            };

        if (place.Id == 0)
        {
            _context.Places.Add(place);
        }

        _context.PlaceExternalReferences.Add(new PlaceExternalReference
        {
            Place = place,
            Provider = ExternalProvider.OpenStreetMap,
            ExternalId = externalId,
            ExternalUrl = $"https://www.openstreetmap.org/{osmType}/{osmId}"
        });

        await _context.SaveChangesAsync();
        return CatalogMapping.ToDto(place);
    }

    public async Task<List<BoardConfigurationDto>> GetBoardConfigurationsAsync()
    {
        var boards = await _context.BoardConfigurations.OrderBy(b => b.Year).ThenBy(b => b.Name).ToListAsync();
        return boards.Select(CatalogMapping.ToDto).ToList();
    }

    public async Task<List<LogEntryDto>> GetLogEntriesAsync(int? userId = null)
    {
        var query = IncludeLogEntrySummary(_context.LogEntries);

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        var entries = await query
            .OrderByDescending(l => l.OccurredAt)
            .ToListAsync();
        return entries.Select(CatalogMapping.ToDto).ToList();
    }

    public async Task<LogEntryDto> CreateLogEntryAsync(CreateLogEntryDto dto, int userId)
    {
        if (!await _context.Climbs.AnyAsync(c => c.Id == dto.ClimbId))
        {
            throw new InvalidOperationException("Climb not found.");
        }

        if (dto.PlaceId.HasValue && !await _context.Places.AnyAsync(p => p.Id == dto.PlaceId.Value))
        {
            throw new InvalidOperationException("Place not found.");
        }

        var entry = new LogEntry
        {
            ClimbId = dto.ClimbId,
            PlaceId = dto.PlaceId,
            UserId = userId,
            OccurredAt = dto.OccurredAt ?? DateTime.UtcNow,
            Status = dto.Status,
            Rating = dto.Rating,
            Notes = dto.Notes
        };

        _context.LogEntries.Add(entry);
        await _context.SaveChangesAsync();

        var saved = await IncludeLogEntrySummary(_context.LogEntries).FirstAsync(l => l.Id == entry.Id);
        return CatalogMapping.ToDto(saved);
    }

    public async Task<SearchResponseDto> SearchAsync(string query, int limit)
    {
        limit = Math.Clamp(limit, 1, 25);
        var response = new SearchResponseDto();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var localTask = RunProviderAsync("local", () => SearchLocalAsync(query, limit), response.Providers);
        var openBetaTask = RunProviderAsync("openbeta", () => _openBetaClient.SearchAsync(query, limit, timeout.Token), response.Providers);

        var providerResults = await Task.WhenAll(localTask, openBetaTask);
        response.Results = providerResults
            .SelectMany(r => r)
            .GroupBy(r => r.Key)
            .Select(g => g.First())
            .Take(limit * 3)
            .ToList();

        return response;
    }

    private async Task<List<SearchResultDto>> SearchLocalAsync(string query, int limit)
    {
        query = query.Trim();
        var climbs = await IncludeClimbSummary(_context.Climbs)
            .Where(c => string.IsNullOrWhiteSpace(query) || EF.Functions.ILike(c.Name, $"%{query}%"))
            .Take(limit)
            .ToListAsync();

        var climbResults = climbs.Select(c => new SearchResultDto
        {
            Key = $"local:climb:{c.Id}",
            ResultType = "climb",
            Name = c.Name,
            Sources = c.ExternalReferences.Select(r => r.Provider.ToString().ToLowerInvariant()).DefaultIfEmpty("local").Distinct().ToArray(),
            Grade = new GradeDto(c.GradeSystem.ToString().ToLowerInvariant(), c.Grade),
            Discipline = c.Discipline,
            PlaceName = c.Place?.Name ?? c.BoardConfiguration?.Name ?? c.CustomLocationName,
            PlaceKind = c.Place?.Kind,
            Coordinates = c.Place?.Latitude != null && c.Place.Longitude != null
                ? new CoordinatesDto(c.Place.Latitude.Value, c.Place.Longitude.Value)
                : c.CustomLocationLatitude.HasValue && c.CustomLocationLongitude.HasValue
                    ? new CoordinatesDto(c.CustomLocationLatitude.Value, c.CustomLocationLongitude.Value)
                : null,
            LocalId = c.Id
        });

        var places = await _context.Places
            .Include(p => p.ExternalReferences)
            .Where(p => string.IsNullOrWhiteSpace(query) || EF.Functions.ILike(p.Name, $"%{query}%"))
            .Take(limit)
            .ToListAsync();

        var placeResults = places.Select(p => new SearchResultDto
        {
            Key = $"local:place:{p.Id}",
            ResultType = "place",
            Name = p.Name,
            Sources = p.ExternalReferences.Select(r => r.Provider.ToString().ToLowerInvariant()).DefaultIfEmpty("local").Distinct().ToArray(),
            PlaceKind = p.Kind,
            Coordinates = p.Latitude != null && p.Longitude != null ? new CoordinatesDto(p.Latitude.Value, p.Longitude.Value) : null,
            LocalId = p.Id
        });

        return climbResults.Concat(placeResults).ToList();
    }

    private async Task<List<SearchResultDto>> RunProviderAsync(
        string provider,
        Func<Task<List<SearchResultDto>>> action,
        Dictionary<string, string> providers)
    {
        try
        {
            var results = await action();
            providers[provider] = "complete";
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search provider {Provider} failed.", provider);
            providers[provider] = "failed";
            return [];
        }
    }

    private static IQueryable<Climb> IncludeClimbSummary(IQueryable<Climb> query)
    {
        return query
            .Include(c => c.Place)
            .Include(c => c.BoardConfiguration)
            .Include(c => c.Setter)
            .Include(c => c.ExternalReferences)
            .Include(c => c.LogEntries);
    }

    private static IQueryable<LogEntry> IncludeLogEntrySummary(IQueryable<LogEntry> query)
    {
        return query
            .Include(l => l.User)
            .Include(l => l.Place)
            .Include(l => l.Climb)!.ThenInclude(c => c!.Place)
            .Include(l => l.Climb)!.ThenInclude(c => c!.BoardConfiguration)
            .Include(l => l.Climb)!.ThenInclude(c => c!.Setter)
            .Include(l => l.Climb)!.ThenInclude(c => c!.ExternalReferences)
            .Include(l => l.Climb)!.ThenInclude(c => c!.LogEntries);
    }

    private async Task<Place?> FindNearbyPlaceAsync(string name, PlaceKind kind, double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        var normalized = CatalogMapping.Normalize(name);
        var candidates = await _context.Places
            .Where(p => p.Kind == kind && p.Latitude.HasValue && p.Longitude.HasValue)
            .ToListAsync();

        return candidates.SingleOrDefault(p =>
            CatalogMapping.Normalize(p.Name) == normalized
            && DistanceMeters(latitude.Value, longitude.Value, p.Latitude!.Value, p.Longitude!.Value) <= 250);
    }

    private static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static Place CreatePlaceEntity(CreatePlaceDto dto)
    {
        if (!dto.Latitude.HasValue || !dto.Longitude.HasValue)
        {
            throw new InvalidOperationException("Mapped places require latitude and longitude.");
        }

        return new Place
        {
            Name = dto.Name.Trim(),
            Kind = dto.Kind,
            ParentPlaceId = dto.ParentPlaceId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Country = dto.Country
        };
    }

    private static void ValidateClimbContext(int? placeId, bool hasBoard, CreateCustomLocationDto? customLocation)
    {
        var contextCount = (placeId.HasValue ? 1 : 0)
            + (hasBoard ? 1 : 0)
            + (customLocation == null ? 0 : 1);
        if (contextCount != 1)
        {
            throw new InvalidOperationException("Exactly one place, custom location, or board configuration is required.");
        }

        if (customLocation != null && string.IsNullOrWhiteSpace(customLocation.Name))
        {
            throw new InvalidOperationException("Custom location name is required.");
        }
    }

    private async Task<BoardConfiguration?> ResolveBoardConfigurationAsync(int? id, string? name)
    {
        if (id.HasValue)
        {
            return await _context.BoardConfigurations.FindAsync(id.Value)
                ?? throw new InvalidOperationException("Board configuration not found.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        var existing = await _context.BoardConfigurations
            .FirstOrDefaultAsync(b => b.Name.ToLower() == trimmed.ToLower());
        if (existing != null)
        {
            return existing;
        }

        var board = new BoardConfiguration
        {
            Name = trimmed,
            Manufacturer = "Custom",
            Year = 0
        };
        _context.BoardConfigurations.Add(board);
        return board;
    }

    private async Task<Setter?> ResolveSetterAsync(int? id, string? name)
    {
        if (id.HasValue)
        {
            return await _context.Setters.FindAsync(id.Value)
                ?? throw new InvalidOperationException("Setter not found.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        return await _setterService.GetSetterByNameAsync(trimmed)
            ?? await _setterService.CreateSetterAsync(new Setter { Name = trimmed });
    }
}
