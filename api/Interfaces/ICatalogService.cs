using api.DTO;
using api.Models;

namespace api.Interfaces;

public interface ICatalogService
{
    Task<List<ClimbSummaryDto>> GetClimbsAsync();
    Task<ClimbSummaryDto?> GetClimbAsync(int id);
    Task<ClimbSummaryDto> CreateManualClimbAsync(CreateManualClimbDto dto);
    Task<ClimbSummaryDto?> ImportOpenBetaClimbAsync(string uuid);
    Task<bool> DeleteClimbAsync(int id);
    Task<List<PlaceSummaryDto>> GetPlacesAsync();
    Task<PlaceSummaryDto> CreatePlaceAsync(CreatePlaceDto dto);
    Task<PlaceSummaryDto?> ImportOsmPlaceAsync(string osmType, string osmId);
    Task<List<BoardConfigurationDto>> GetBoardConfigurationsAsync();
    Task<List<LogEntryDto>> GetLogEntriesAsync();
    Task<LogEntryDto> CreateLogEntryAsync(CreateLogEntryDto dto);
    Task<SearchResponseDto> SearchAsync(string query, int limit);
}
