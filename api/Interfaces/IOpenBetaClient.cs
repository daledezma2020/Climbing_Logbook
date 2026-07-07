using api.DTO;

namespace api.Interfaces;

public interface IOpenBetaClient
{
    Task<List<SearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken);
    Task<OpenBetaClimbDetails?> GetClimbAsync(string uuid, CancellationToken cancellationToken);
}

public class OpenBetaClimbDetails
{
    public string Uuid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Grade { get; set; }
    public string? FirstAscentName { get; set; }
    public string? ParentAreaUuid { get; set; }
    public string? ParentAreaName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
