using api.DTO;
using api.Models;

namespace api.Services;

public static class CatalogMapping
{
    public static PlaceSummaryDto ToDto(Place place) => new()
    {
        Id = place.Id,
        Name = place.Name,
        Kind = place.Kind,
        Latitude = place.Latitude,
        Longitude = place.Longitude,
        Address = place.Address,
        City = place.City,
        State = place.State,
        Country = place.Country
    };

    public static BoardConfigurationDto ToDto(BoardConfiguration board) => new()
    {
        Id = board.Id,
        Name = board.Name,
        Manufacturer = board.Manufacturer,
        Year = board.Year
    };

    public static ClimbSummaryDto ToDto(Climb climb) => new()
    {
        Id = climb.Id,
        Name = climb.Name,
        Discipline = climb.Discipline,
        GradeSystem = climb.GradeSystem,
        Grade = climb.Grade,
        PlaceId = climb.PlaceId,
        Place = climb.Place == null ? null : ToDto(climb.Place),
        BoardConfigurationId = climb.BoardConfigurationId,
        BoardConfiguration = climb.BoardConfiguration == null ? null : ToDto(climb.BoardConfiguration),
        CustomLocation = string.IsNullOrWhiteSpace(climb.CustomLocationName)
            || !climb.CustomLocationLatitude.HasValue
            || !climb.CustomLocationLongitude.HasValue
                ? null
                : new CustomLocationDto
                {
                    Name = climb.CustomLocationName,
                    Latitude = climb.CustomLocationLatitude.Value,
                    Longitude = climb.CustomLocationLongitude.Value
                },
        SetterId = climb.SetterId,
        SetterName = climb.Setter?.Name,
        FirstAscentName = climb.FirstAscentName,
        PictureUrl = climb.PictureUrl,
        VideoUrl = climb.VideoUrl,
        AverageRating = climb.LogEntries.Any(l => l.Rating.HasValue)
            ? (decimal)climb.LogEntries.Where(l => l.Rating.HasValue).Average(l => l.Rating!.Value)
            : 0,
        Sources = climb.ExternalReferences.Select(r => r.Provider.ToString()).Distinct().ToArray()
    };

    public static LogEntryDto ToDto(LogEntry entry) => new()
    {
        Id = entry.Id,
        ClimbId = entry.ClimbId,
        Climb = entry.Climb == null ? null : ToDto(entry.Climb),
        PlaceId = entry.PlaceId,
        Place = entry.Place == null ? null : ToDto(entry.Place),
        OccurredAt = entry.OccurredAt,
        Status = entry.Status,
        Rating = entry.Rating,
        Notes = entry.Notes,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
