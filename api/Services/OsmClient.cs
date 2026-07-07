using api.DTO;
using api.Interfaces;
using api.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace api.Services;

public class OsmClient : IOsmClient
{
    private const string DefaultOverpassUrl = "https://overpass-api.de/api/interpreter";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OsmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<List<SearchResultDto>> SearchPlacesAsync(string query, string bbox, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bbox))
        {
            return [];
        }

        var overpassUrl = _configuration["Osm:OverpassUrl"] ?? DefaultOverpassUrl;
        var parsedBbox = ParseBbox(bbox);
        var nameFilter = string.IsNullOrWhiteSpace(query)
            ? string.Empty
            : $"[\"name\"~\"{EscapeOverpassRegex(query)}\",i]";

        var overpassQuery = $"""
            [out:json][timeout:8];
            (
              nwr["sport"="climbing"]{nameFilter}({parsedBbox});
              nwr["leisure"="sports_centre"]["sport"="climbing"]{nameFilter}({parsedBbox});
              nwr["climbing"]{nameFilter}({parsedBbox});
            );
            out center tags {Math.Clamp(limit, 1, 25)};
            """;

        using var response = await _httpClient.PostAsync(
            overpassUrl,
            new StringContent($"data={Uri.EscapeDataString(overpassQuery)}", Encoding.UTF8, "application/x-www-form-urlencoded"),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var results = new List<SearchResultDto>();
        foreach (var element in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            var tags = element.TryGetProperty("tags", out var tagsElement) ? tagsElement : default;
            var name = tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var osmType = element.GetProperty("type").GetString() ?? "node";
            var osmId = element.GetProperty("id").GetRawText();
            var lat = ReadCoordinate(element, "lat") ?? ReadCenterCoordinate(element, "lat");
            var lon = ReadCoordinate(element, "lon") ?? ReadCenterCoordinate(element, "lon");
            var kind = IsGym(tags) ? PlaceKind.Gym : PlaceKind.Outdoor;

            results.Add(new SearchResultDto
            {
                Key = $"osm:place:{osmType}/{osmId}",
                ResultType = "place",
                Name = name,
                Sources = ["osm"],
                PlaceKind = kind,
                Coordinates = lat.HasValue && lon.HasValue ? new CoordinatesDto(lat.Value, lon.Value) : null,
                ExternalId = $"{osmType}/{osmId}"
            });
        }

        return results;
    }

    public async Task<OsmPlaceDetails?> GetPlaceAsync(string osmType, string osmId, CancellationToken cancellationToken)
    {
        var overpassUrl = _configuration["Osm:OverpassUrl"] ?? DefaultOverpassUrl;
        var selector = $"{osmType.ToLowerInvariant()}({osmId});";
        var overpassQuery = $"""
            [out:json][timeout:8];
            {selector}
            out center tags;
            """;

        using var response = await _httpClient.PostAsync(
            overpassUrl,
            new StringContent($"data={Uri.EscapeDataString(overpassQuery)}", Encoding.UTF8, "application/x-www-form-urlencoded"),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var element = doc.RootElement.GetProperty("elements").EnumerateArray().FirstOrDefault();
        if (element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        var tags = element.TryGetProperty("tags", out var tagsElement) ? tagsElement : default;
        var name = ReadTag(tags, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new OsmPlaceDetails
        {
            OsmType = osmType,
            OsmId = osmId,
            Name = name,
            Latitude = ReadCoordinate(element, "lat") ?? ReadCenterCoordinate(element, "lat"),
            Longitude = ReadCoordinate(element, "lon") ?? ReadCenterCoordinate(element, "lon"),
            Address = ReadTag(tags, "addr:street"),
            City = ReadTag(tags, "addr:city"),
            State = ReadTag(tags, "addr:state"),
            Country = ReadTag(tags, "addr:country")
        };
    }

    private static string ParseBbox(string bbox)
    {
        var values = bbox.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length != 4)
        {
            throw new ArgumentException("bbox must be west,south,east,north");
        }

        var west = double.Parse(values[0], CultureInfo.InvariantCulture);
        var south = double.Parse(values[1], CultureInfo.InvariantCulture);
        var east = double.Parse(values[2], CultureInfo.InvariantCulture);
        var north = double.Parse(values[3], CultureInfo.InvariantCulture);

        return string.Join(',', new[] { south, west, north, east }.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }

    private static string EscapeOverpassRegex(string value)
    {
        return Regex.Escape(value).Replace("\"", "\\\"");
    }

    private static bool IsGym(JsonElement tags)
    {
        return ReadTag(tags, "leisure") == "sports_centre"
            || ReadTag(tags, "climbing:indoor") == "yes"
            || ReadTag(tags, "indoor") == "yes";
    }

    private static string? ReadTag(JsonElement tags, string key)
    {
        return tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty(key, out var value)
            ? value.GetString()
            : null;
    }

    private static double? ReadCoordinate(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
    }

    private static double? ReadCenterCoordinate(JsonElement element, string property)
    {
        return element.TryGetProperty("center", out var center) && center.TryGetProperty(property, out var value)
            ? value.GetDouble()
            : null;
    }
}
