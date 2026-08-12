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

    public UserService CreateUserService(IAuth0UserInfoClient? userInfo = null, string? bearerToken = null)
    {
        var httpContextAccessor = new HttpContextAccessor();
        if (bearerToken != null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Authorization = $"Bearer {bearerToken}";
            httpContextAccessor.HttpContext = httpContext;
        }

        return new UserService(
            CreateContext(),
            userInfo ?? new StubAuth0UserInfoClient(),
            httpContextAccessor,
            NullLogger<UserService>.Instance);
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
