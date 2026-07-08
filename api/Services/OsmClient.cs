using api.Interfaces;
using System.Text;
using System.Text.Json;

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
