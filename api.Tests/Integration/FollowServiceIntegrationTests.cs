using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class FollowServiceIntegrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FollowingTheSameUserTwiceKeepsASingleRow()
    {
        var alex = await database.CreateUserAsync("alex");
        var sam = await database.CreateUserAsync("sam");

        await database.CreateFollowService().FollowAsync(alex, sam);
        await database.CreateFollowService().FollowAsync(alex, sam);

        await using var context = database.CreateContext();
        var follow = await context.Follows.SingleAsync();

        Assert.Equal(alex, follow.FollowerId);
        Assert.Equal(sam, follow.FolloweeId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnfollowingIsIdempotentAndRemovesTheRelationship()
    {
        var alex = await database.CreateUserAsync("alex");
        var sam = await database.CreateUserAsync("sam");

        await database.CreateFollowService().FollowAsync(alex, sam);
        await database.CreateFollowService().UnfollowAsync(alex, sam);
        await database.CreateFollowService().UnfollowAsync(alex, sam);

        await using var context = database.CreateContext();
        Assert.Empty(await context.Follows.ToListAsync());
        Assert.False(await database.CreateFollowService().IsFollowingAsync(alex, sam));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SelfFollowIsRejectedByTheServiceAndByTheDatabase()
    {
        var alex = await database.CreateUserAsync("alex");

        await Assert.ThrowsAsync<SelfFollowException>(
            () => database.CreateFollowService().FollowAsync(alex, alex));

        await using var context = database.CreateContext();
        context.Follows.Add(new Follow { FollowerId = alex, FolloweeId = alex });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CountsReflectBothDirectionsOfTheRelationship()
    {
        var alex = await database.CreateUserAsync("alex");
        var sam = await database.CreateUserAsync("sam");
        var jo = await database.CreateUserAsync("jo");

        await database.CreateFollowService().FollowAsync(alex, sam);
        await database.CreateFollowService().FollowAsync(jo, sam);
        await database.CreateFollowService().FollowAsync(sam, alex);

        var samCounts = await database.CreateFollowService().GetCountsAsync(sam);
        var alexCounts = await database.CreateFollowService().GetCountsAsync(alex);

        Assert.Equal(2, samCounts.Followers);
        Assert.Equal(1, samCounts.Following);
        Assert.Equal(1, alexCounts.Followers);
        Assert.Equal(1, alexCounts.Following);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FollowerAndFollowingPagesReportTheTotalAndHonourSkipAndTake()
    {
        var sam = await database.CreateUserAsync("sam");
        for (var i = 0; i < 3; i++)
        {
            var follower = await database.CreateUserAsync($"follower{i}");
            await database.CreateFollowService().FollowAsync(follower, sam);
        }

        var firstPage = await database.CreateFollowService().GetFollowersAsync(sam, null, 0, 2);
        var secondPage = await database.CreateFollowService().GetFollowersAsync(sam, null, 2, 2);

        Assert.Equal(3, firstPage.Total);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Empty(firstPage.Items.Select(i => i.Username).Intersect(secondPage.Items.Select(i => i.Username)));

        var following = await database.CreateFollowService().GetFollowingAsync(sam, null, 0, 25);
        Assert.Equal(0, following.Total);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConnectionListsFlagWhoTheCallerAlreadyFollows()
    {
        var caller = await database.CreateUserAsync("caller");
        var sam = await database.CreateUserAsync("sam");
        var alex = await database.CreateUserAsync("alex");
        var jo = await database.CreateUserAsync("jo");

        await database.CreateFollowService().FollowAsync(alex, sam);
        await database.CreateFollowService().FollowAsync(jo, sam);
        await database.CreateFollowService().FollowAsync(caller, alex);

        var page = await database.CreateFollowService().GetFollowersAsync(sam, caller, 0, 25);

        Assert.True(page.Items.Single(i => i.Username == "alex").IsFollowedByMe);
        Assert.False(page.Items.Single(i => i.Username == "jo").IsFollowedByMe);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchMatchesPartialUsernamesAndDisplayNamesCaseInsensitively()
    {
        await database.CreateUserAsync("alexclimbs", "Alex Honnold");
        await database.CreateUserAsync("samb", "Sam Boulders");
        await database.CreateUserAsync("jo", "Jo Nobody");

        var byUsername = await database.CreateUserService().SearchUsersAsync("ALEXcl", 10, null);
        var byDisplayName = await database.CreateUserService().SearchUsersAsync("bouLDers", 10, null);
        var noMatch = await database.CreateUserService().SearchUsersAsync("zzz", 10, null);
        var blank = await database.CreateUserService().SearchUsersAsync("   ", 10, null);

        Assert.Equal("alexclimbs", Assert.Single(byUsername).Username);
        Assert.Equal("samb", Assert.Single(byDisplayName).Username);
        Assert.Empty(noMatch);
        Assert.Empty(blank);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchHonoursTheLimitAndReportsTheCallersOwnRowAndFollowState()
    {
        var caller = await database.CreateUserAsync("caller", "Caller Person");
        await database.CreateUserAsync("climber1", "Person One");
        await database.CreateUserAsync("climber2", "Person Two");
        var third = await database.CreateUserAsync("climber3", "Person Three");

        await database.CreateFollowService().FollowAsync(caller, third);

        var limited = await database.CreateUserService().SearchUsersAsync("Person", 2, caller);
        Assert.Equal(2, limited.Count);

        var all = await database.CreateUserService().SearchUsersAsync("Person", 10, caller);
        Assert.True(all.Single(u => u.Username == "caller").IsMe);
        Assert.True(all.Single(u => u.Username == "climber3").IsFollowedByMe);
        Assert.False(all.Single(u => u.Username == "climber1").IsFollowedByMe);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProfilesCarryFollowCountsAndTheCallersRelationship()
    {
        var caller = await database.CreateUserAsync("caller");
        var sam = await database.CreateUserAsync("sam");
        await database.CreateFollowService().FollowAsync(caller, sam);

        await using var context = database.CreateContext();
        var samUser = await context.AppUsers.SingleAsync(u => u.Id == sam);
        var callerUser = await context.AppUsers.SingleAsync(u => u.Id == caller);

        var asVisitor = await database.CreateUserService().GetProfileAsync(samUser, null);
        var asCaller = await database.CreateUserService().GetProfileAsync(samUser, callerUser);
        var asSelf = await database.CreateUserService().GetProfileAsync(samUser, samUser);

        Assert.Equal(1, asVisitor.FollowerCount);
        Assert.False(asVisitor.IsFollowedByMe);
        Assert.False(asVisitor.IsMe);

        Assert.True(asCaller.IsFollowedByMe);
        Assert.False(asCaller.IsMe);

        Assert.True(asSelf.IsMe);
        Assert.False(asSelf.IsFollowedByMe);
        Assert.Equal(0, asSelf.FollowingCount);
    }
}
