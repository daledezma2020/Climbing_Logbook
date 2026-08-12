using api.DTO;
using api.Models;
using System.Security.Claims;

namespace api.Interfaces;

public interface IUserService
{
    Task<AppUser> EnsureUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<AppUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(AppUser user, bool includePrivate, CancellationToken cancellationToken = default);
    Task<AppUser> UpdateProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default);
    Task<AppUser> SetAvatarAsync(int userId, IFormFile file, CancellationToken cancellationToken = default);
}
