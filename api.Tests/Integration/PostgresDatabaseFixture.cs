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
        return CreateCatalogService(CreateContext(), openBeta, osm);
    }

    private static CatalogService CreateCatalogService(
        ApplicationDbContext context,
        IOpenBetaClient? openBeta = null,
        IOsmClient? osm = null)
    {
        return new CatalogService(
            context,
            openBeta ?? new StubOpenBetaClient(),
            osm ?? new StubOsmClient(),
            new SetterService(context),
            new FollowService(context),
            NullLogger<CatalogService>.Instance);
    }

    public SocialService CreateSocialService()
    {
        var context = CreateContext();
        return new SocialService(
            context,
            CreateCatalogService(context),
            new FollowService(context));
    }

    public async Task<int> CreateClimbAsync(
        string name = "Test Climb",
        GradeSystem gradeSystem = GradeSystem.VScale,
        string grade = "V4",
        int? placeId = null)
    {
        await using var context = CreateContext();
        var climb = new Climb
        {
            Name = name,
            GradeSystem = gradeSystem,
            Grade = grade,
            CustomLocationName = placeId.HasValue ? null : "Test Crag",
            CustomLocationLatitude = placeId.HasValue ? null : 40.0,
            CustomLocationLongitude = placeId.HasValue ? null : -105.0,
            PlaceId = placeId
        };
        context.Climbs.Add(climb);
        await context.SaveChangesAsync();
        return climb.Id;
    }

    public async Task<int> CreateLogEntryAsync(
        int userId,
        int climbId,
        DateTime? occurredAt = null,
        LogEntryStatus status = LogEntryStatus.Completed)
    {
        await using var context = CreateContext();
        var entry = new LogEntry
        {
            UserId = userId,
            ClimbId = climbId,
            OccurredAt = occurredAt ?? DateTime.UtcNow,
            Status = status
        };
        context.LogEntries.Add(entry);
        await context.SaveChangesAsync();
        return entry.Id;
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
        var catalogService = CreateCatalogService(context);

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
