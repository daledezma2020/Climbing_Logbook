using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class UserServiceIntegrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FirstContactProvisionsExactlyOneUserAndRepeatContactReusesIt()
    {
        var principal = Principal("auth0|123", name: "Alex Climber", email: "alex@example.test");

        var created = await database.CreateUserService().EnsureUserAsync(principal);
        var second = await database.CreateUserService().EnsureUserAsync(principal);

        await using var context = database.CreateContext();
        var stored = await context.AppUsers.SingleAsync();

        Assert.Equal(created.Id, second.Id);
        Assert.Equal(stored.Id, created.Id);
        Assert.Equal("auth0|123", stored.Auth0Subject);
        Assert.Equal("Alex Climber", stored.DisplayName);
        Assert.Equal("alex@example.test", stored.Email);
        Assert.Equal("alex", stored.Username);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UsernamesAreDerivedFromTheEmailLocalPartAndDeduplicated()
    {
        var first = await database.CreateUserService()
            .EnsureUserAsync(Principal("auth0|1", name: "Alex One", email: "alex@example.test"));
        var second = await database.CreateUserService()
            .EnsureUserAsync(Principal("auth0|2", name: "Alex Two", email: "alex@other.test"));
        var third = await database.CreateUserService()
            .EnsureUserAsync(Principal("auth0|3", name: "Alex Three", email: "ALEX@third.test"));

        Assert.Equal("alex", first.Username);
        Assert.Equal("alex2", second.Username);
        Assert.Equal("alex3", third.Username);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UsernamesAreUniqueCaseInsensitivelyAtTheDatabaseLevel()
    {
        await using var context = database.CreateContext();
        context.AppUsers.AddRange(
            new AppUser { Auth0Subject = "auth0|1", Username = "alex", DisplayName = "Alex" },
            new AppUser { Auth0Subject = "auth0|2", Username = "ALEX", DisplayName = "Alex Again" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Auth0SubjectIsUniqueAtTheDatabaseLevel()
    {
        await using var context = database.CreateContext();
        context.AppUsers.AddRange(
            new AppUser { Auth0Subject = "auth0|duplicate", Username = "one", DisplayName = "One" },
            new AppUser { Auth0Subject = "auth0|duplicate", Username = "two", DisplayName = "Two" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProfileFallsBackToUserInfoWhenTheAccessTokenOmitsProfileClaims()
    {
        var userInfo = new StubAuth0UserInfoClient
        {
            UserInfo = new Auth0UserInfo
            {
                Name = "Alex From UserInfo",
                Email = "alex@userinfo.test",
                Picture = "https://example.test/alex.png"
            }
        };

        var user = await database.CreateUserService(userInfo, bearerToken: "token")
            .EnsureUserAsync(Principal("auth0|123"));

        Assert.Equal(1, userInfo.CallCount);
        Assert.Equal("Alex From UserInfo", user.DisplayName);
        Assert.Equal("alex@userinfo.test", user.Email);
        Assert.Equal("https://example.test/alex.png", user.PictureUrl);
        Assert.Equal("alex", user.Username);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProvisioningSurvivesAUserInfoOutageAndAnIdentityWithNoProfile()
    {
        var user = await database.CreateUserService(bearerToken: "token")
            .EnsureUserAsync(Principal("auth0|no-profile"));

        Assert.False(string.IsNullOrWhiteSpace(user.Username));
        Assert.Equal(user.Username, user.DisplayName);
        Assert.Null(user.Email);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ATokenWithoutASubjectClaimIsRejected()
    {
        var service = database.CreateUserService();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnsureUserAsync(new ClaimsPrincipal(new ClaimsIdentity([], "test"))));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LogEntriesAreScopedToTheirOwnerButRemainVisibleUnfiltered()
    {
        var catalog = database.CreateCatalogService();
        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Shared Problem",
            Grade = "V3",
            BoardConfigurationName = "Board"
        });

        var alex = await database.CreateUserAsync("alex");
        var sam = await database.CreateUserAsync("sam");
        await catalog.CreateLogEntryAsync(new CreateLogEntryDto { ClimbId = climb.Id, Rating = 4 }, alex);
        await catalog.CreateLogEntryAsync(new CreateLogEntryDto { ClimbId = climb.Id, Rating = 2 }, sam);

        var alexEntries = await catalog.GetLogEntriesAsync(alex);
        var everything = await catalog.GetLogEntriesAsync();

        Assert.Equal(alex, Assert.Single(alexEntries).UserId);
        Assert.Equal("alex", Assert.Single(alexEntries).User?.Username);
        Assert.Equal(2, everything.Count);
        Assert.Empty(await catalog.GetLogEntriesAsync(9999));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeletingAUserRemovesTheirLogEntries()
    {
        var catalog = database.CreateCatalogService();
        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Owned Problem",
            Grade = "V1",
            BoardConfigurationName = "Board"
        });
        var userId = await database.CreateUserAsync("alex");
        await catalog.CreateLogEntryAsync(new CreateLogEntryDto { ClimbId = climb.Id }, userId);

        await using (var context = database.CreateContext())
        {
            context.AppUsers.Remove(await context.AppUsers.SingleAsync(u => u.Id == userId));
            await context.SaveChangesAsync();
        }

        Assert.Empty(await catalog.GetLogEntriesAsync());
    }

    private static ClaimsPrincipal Principal(string subject, string? name = null, string? email = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, subject) };
        if (name != null)
        {
            claims.Add(new Claim("name", name));
        }

        if (email != null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }
}
