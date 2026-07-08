namespace api.Interfaces;

public interface IOsmClient
{
    Task<OsmPlaceDetails?> GetPlaceAsync(string osmType, string osmId, CancellationToken cancellationToken);
}

public class OsmPlaceDetails
{
    public string OsmType { get; set; } = string.Empty;
    public string OsmId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}
