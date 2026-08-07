using api.Models;
using System.Security.Claims;

namespace api.Interfaces;

public interface IUserService
{
    Task<AppUser> EnsureUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
