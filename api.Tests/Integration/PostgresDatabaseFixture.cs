using api.DTO;
using api.Interfaces;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace api.Tests.Integration;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresDatabaseFixture>
{
    public const string Name = "PostgreSQL";
}

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public PostgresDatabaseFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("CLIMBING_LOGBOOK_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Set CLIMBING_LOGBOOK_TEST_CONNECTION_STRING to a dedicated PostgreSQL test database before running integration tests.");

        var connection = new NpgsqlConnectionStringBuilder(ConnectionString);
        if (string.IsNullOrWhiteSpace(connection.Database)
            || !connection.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The integration database name must contain 'test'.");
        }

        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
    }

    public string ConnectionString { get; }

    public ApplicationDbContext CreateContext() => new(_options);

    public CatalogService CreateCatalogService(
        IOpenBetaClient? openBeta = null,
        IOsmClient? osm = null)
    {
        var context = CreateContext();
        return new CatalogService(
            context,
            openBeta ?? new StubOpenBetaClient(),
            osm ?? new StubOsmClient(),
            new SetterService(context),
            NullLogger<CatalogService>.Instance);
    }

    public async Task<int> CreateUserAsync(string username = "testclimber")
    {
        await using var context = CreateContext();
        var user = new AppUser
        {
            Auth0Subject = $"auth0|{username}",
            Username = username,
            DisplayName = username
        };
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    public UserService CreateUserService(
        IAuth0UserInfoClient? userInfo = null,
        string? bearerToken = null,
        IAvatarStorage? avatarStorage = null)
    {
        var httpContextAccessor = new HttpContextAccessor();
        if (bearerToken != null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Authorization = $"Bearer {bearerToken}";
            httpContextAccessor.HttpContext = httpContext;
        }

        var context = CreateContext();
        var catalogService = new CatalogService(
            context,
            new StubOpenBetaClient(),
            new StubOsmClient(),
            new SetterService(context),
            NullLogger<CatalogService>.Instance);

        return new UserService(
            context,
            userInfo ?? new StubAuth0UserInfoClient(),
            catalogService,
            new FollowService(context),
            avatarStorage ?? new StubAvatarStorage(),
            httpContextAccessor,
            NullLogger<UserService>.Instance);
    }

    public FollowService CreateFollowService() => new(CreateContext());

    public async Task<int> CreateUserAsync(string username, string displayName)
    {
        await using var context = CreateContext();
        var user = new AppUser
        {
            Auth0Subject = $"auth0|{username}",
            Username = username,
            DisplayName = displayName
        };
        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public Task InitializeAsync() => ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

internal sealed class StubOpenBetaClient : IOpenBetaClient
{
    public OpenBetaClimbDetails? Climb { get; set; }
    public List<SearchResultDto> SearchResults { get; set; } = [];
    public Exception? SearchException { get; set; }
    public int? LastSearchLimit { get; private set; }

    public Task<List<SearchResultDto>> SearchAsync(string query, int limit, CancellationToken cancellationToken)
    {
        LastSearchLimit = limit;
        if (SearchException != null)
        {
            throw SearchException;
        }

        return Task.FromResult(SearchResults.Take(limit).ToList());
    }

    public Task<OpenBetaClimbDetails?> GetClimbAsync(string uuid, CancellationToken cancellationToken)
        => Task.FromResult(Climb);
}

internal sealed class StubOsmClient : IOsmClient
{
    public OsmPlaceDetails? Place { get; set; }

    public Task<OsmPlaceDetails?> GetPlaceAsync(string osmType, string osmId, CancellationToken cancellationToken)
        => Task.FromResult(Place);
}

internal sealed class StubAuth0UserInfoClient : IAuth0UserInfoClient
{
    public Auth0UserInfo? UserInfo { get; set; }
    public int CallCount { get; private set; }

    public Task<Auth0UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(UserInfo);
    }
}

internal sealed class StubAvatarStorage : IAvatarStorage
{
    public string SavedUrl { get; set; } = "https://localhost/uploads/avatars/stub.png";
    public string? LastDeletedUrl { get; private set; }

    public Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
        => Task.FromResult(SavedUrl);

    public Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        LastDeletedUrl = url;
        return Task.CompletedTask;
    }
}
