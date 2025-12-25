using api.Models;

namespace api.Interfaces;

public interface IClimbRouteService
{
    Task<IEnumerable<ClimbRoute>> GetAllClimbRoutesAsync();
    Task<ClimbRoute?> GetClimbRouteByIdAsync(int id);
    Task<ClimbRoute> CreateClimbRouteAsync(ClimbRoute climbRoute);
    Task<ClimbRoute?> UpdateClimbRouteAsync(int id, ClimbRoute climbRoute);
    Task<bool> DeleteClimbRouteAsync(int id);
    Task<bool> ClimbRouteExistsAsync(int id);
}
