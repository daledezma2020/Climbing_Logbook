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
    public List<UserSummaryDto> SearchResults { get; set; } = [];
    public ClaimsPrincipal? LastPrincipal { get; private set; }
    public string? LastRequestedUsername { get; private set; }
    public AppUser? LastCaller { get; private set; }
    public string? LastSearchQuery { get; private set; }
    public int? LastSearchLimit { get; private set; }
    public int? LastSearchCallerId { get; private set; }

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

    public Task<UserProfileDto> GetProfileAsync(AppUser user, AppUser? caller, CancellationToken cancellationToken = default)
    {
        LastCaller = caller;
        return Task.FromResult(Profile);
    }

    public Task<List<UserSummaryDto>> SearchUsersAsync(
        string query,
        int limit,
        int? callerId,
        CancellationToken cancellationToken = default)
    {
        LastSearchQuery = query;
        LastSearchLimit = limit;
        LastSearchCallerId = callerId;
        return Task.FromResult(SearchResults);
    }

    public Task<AppUser> UpdateProfileAsync(int userId, UpdateUserProfileDto dto, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);

    public Task<AppUser> SetAvatarAsync(int userId, IFormFile file, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);
}
