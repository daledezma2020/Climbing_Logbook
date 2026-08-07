using api.Interfaces;
using api.Models;
using System.Security.Claims;

namespace api.Tests.Unit;

internal sealed class UserServiceStub : IUserService
{
    public AppUser User { get; set; } = new();
    public ClaimsPrincipal? LastPrincipal { get; private set; }

    public Task<AppUser> EnsureUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        LastPrincipal = principal;
        return Task.FromResult(User);
    }
}
