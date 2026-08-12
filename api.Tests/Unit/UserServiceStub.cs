using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace api.Tests.Unit;

internal sealed class UserServiceStub : IUserService
{
    public AppUser User { get; set; } = new();
    public AppUser? FoundUser { get; set; }
    public UserProfileDto Profile { get; set; } = new();
    public ClaimsPrincipal? LastPrincipal { get; private set; }
    public string? LastRequestedUsername { get; private set; }
    public bool LastIncludePrivate { get; private set; }

    public Task<AppUser> EnsureUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        LastPrincipal = principal;
        return Task.FromResult(User);
    }

    public Task<AppUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        LastRequestedUsername = username;
        return Task.FromResult(FoundUser);
    }

    public Task<UserProfileDto> GetProfileAsync(AppUser user, bool includePrivate, CancellationToken cancellationToken = default)
    {
        LastIncludePrivate = includePrivate;
        return Task.FromResult(Profile);
    }

    public Task<AppUser> UpdateProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);

    public Task<AppUser> SetAvatarAsync(int userId, IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);
}
