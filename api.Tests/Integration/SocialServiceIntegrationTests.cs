using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class SocialServiceIntegrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TheFeedCarriesEntriesFromFollowedClimbersAndTheCallerButNotStrangers()
    {
        var me = await database.CreateUserAsync("me");
        var friend = await database.CreateUserAsync("friend");
        var stranger = await database.CreateUserAsync("stranger");
        var climbId = await database.CreateClimbAsync();

        await database.CreateFollowService().FollowAsync(me, friend);

        await database.CreateLogEntryAsync(me, climbId, DateTime.UtcNow.AddDays(-1));
        await database.CreateLogEntryAsync(friend, climbId, DateTime.UtcNow);
        await database.CreateLogEntryAsync(stranger, climbId, DateTime.UtcNow);

        var feed = await database.CreateSocialService().GetFeedAsync(me, 0, 25);

        Assert.Equal(2, feed.Total);
        Assert.All(feed.Items, entry => Assert.NotEqual(stranger, entry.UserId));
        // Newest first: the friend's entry is today, the caller's was yesterday.
        Assert.Equal(friend, feed.Items[0].UserId);
        Assert.Equal(me, feed.Items[1].UserId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AnEmptyFollowGraphStillReturnsTheCallersOwnEntries()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        await database.CreateLogEntryAsync(me, climbId);

        var feed = await database.CreateSocialService().GetFeedAsync(me, 0, 25);

        Assert.Equal(1, feed.Total);
        Assert.Equal(me, Assert.Single(feed.Items).UserId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LikingTheSameEntryTwiceKeepsASingleRowAndUnlikingRemovesIt()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        Assert.True(await database.CreateSocialService().LikeAsync(me, entryId));
        Assert.True(await database.CreateSocialService().LikeAsync(me, entryId));

        await using (var context = database.CreateContext())
        {
            Assert.Equal(1, await context.Likes.CountAsync(l => l.LogEntryId == entryId));
        }

        Assert.True(await database.CreateSocialService().UnlikeAsync(me, entryId));

        await using (var context = database.CreateContext())
        {
            Assert.Equal(0, await context.Likes.CountAsync(l => l.LogEntryId == entryId));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UnlikingSomethingYouNeverLikedSucceedsWithoutChangingAnything()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        Assert.True(await database.CreateSocialService().UnlikeAsync(me, entryId));

        await using var context = database.CreateContext();
        Assert.Equal(0, await context.Likes.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LikesAndCommentsOnAMissingLogEntryAreReported()
    {
        var me = await database.CreateUserAsync("me");

        Assert.False(await database.CreateSocialService().LikeAsync(me, 9999));
        Assert.False(await database.CreateSocialService().UnlikeAsync(me, 9999));
        Assert.Null(await database.CreateSocialService()
            .AddLogEntryCommentAsync(me, 9999, new CreateCommentDto { Content = "hi" }));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CommentsAttachToEitherALogEntryOrAClimb()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        var onEntry = await database.CreateSocialService()
            .AddLogEntryCommentAsync(me, entryId, new CreateCommentDto { Content = "strong work" });
        var onClimb = await database.CreateSocialService()
            .AddClimbCommentAsync(me, climbId, new CreateCommentDto { Content = "sandbagged" });

        Assert.NotNull(onEntry);
        Assert.Equal(entryId, onEntry!.LogEntryId);
        Assert.Null(onEntry.ClimbId);

        Assert.NotNull(onClimb);
        Assert.Equal(climbId, onClimb!.ClimbId);
        Assert.Null(onClimb.LogEntryId);

        var entryComments = await database.CreateSocialService()
            .GetLogEntryCommentsAsync(entryId, me, 0, 25);
        var climbComments = await database.CreateSocialService()
            .GetClimbCommentsAsync(climbId, me, 0, 25);

        Assert.Equal("strong work", Assert.Single(entryComments.Items).Content);
        Assert.Equal("sandbagged", Assert.Single(climbComments.Items).Content);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ACommentWithNoTargetOrTwoTargetsViolatesTheCheckConstraint()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        await using (var context = database.CreateContext())
        {
            context.Comments.Add(new Comment { UserId = me, Content = "orphan" });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = database.CreateContext())
        {
            context.Comments.Add(new Comment
            {
                UserId = me,
                Content = "both",
                ClimbId = climbId,
                LogEntryId = entryId
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OnlyTheAuthorCanDeleteAComment()
    {
        var me = await database.CreateUserAsync("me");
        var other = await database.CreateUserAsync("other");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        var comment = await database.CreateSocialService()
            .AddLogEntryCommentAsync(me, entryId, new CreateCommentDto { Content = "mine" });

        Assert.Equal(
            CommentDeletion.Forbidden,
            await database.CreateSocialService().DeleteCommentAsync(other, comment!.Id));
        Assert.Equal(
            CommentDeletion.NotFound,
            await database.CreateSocialService().DeleteCommentAsync(me, 9999));
        Assert.Equal(
            CommentDeletion.Deleted,
            await database.CreateSocialService().DeleteCommentAsync(me, comment.Id));

        await using var context = database.CreateContext();
        Assert.Equal(0, await context.Comments.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CommentAuthorshipDrivesTheDeleteAffordance()
    {
        var me = await database.CreateUserAsync("me");
        var other = await database.CreateUserAsync("other");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        await database.CreateSocialService()
            .AddLogEntryCommentAsync(me, entryId, new CreateCommentDto { Content = "mine" });

        var asAuthor = await database.CreateSocialService().GetLogEntryCommentsAsync(entryId, me, 0, 25);
        var asOther = await database.CreateSocialService().GetLogEntryCommentsAsync(entryId, other, 0, 25);
        var asVisitor = await database.CreateSocialService().GetLogEntryCommentsAsync(entryId, null, 0, 25);

        Assert.True(Assert.Single(asAuthor.Items).CanDelete);
        Assert.False(Assert.Single(asOther.Items).CanDelete);
        Assert.False(Assert.Single(asVisitor.Items).CanDelete);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FeedEntriesCarryLikeAndCommentCountsAndTheCallersOwnLikeState()
    {
        var me = await database.CreateUserAsync("me");
        var friend = await database.CreateUserAsync("friend");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        await database.CreateSocialService().LikeAsync(me, entryId);
        await database.CreateSocialService().LikeAsync(friend, entryId);
        await database.CreateSocialService()
            .AddLogEntryCommentAsync(friend, entryId, new CreateCommentDto { Content = "nice" });

        var mine = await database.CreateSocialService().GetFeedAsync(me, 0, 25);
        var entry = Assert.Single(mine.Items);

        Assert.Equal(2, entry.LikeCount);
        Assert.Equal(1, entry.CommentCount);
        Assert.True(entry.IsLikedByMe);

        // A climb-level comment must not inflate the log entry's comment count.
        await database.CreateSocialService()
            .AddClimbCommentAsync(friend, climbId, new CreateCommentDto { Content = "on the climb" });

        var refreshed = await database.CreateSocialService().GetFeedAsync(me, 0, 25);
        Assert.Equal(1, Assert.Single(refreshed.Items).CommentCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeletingALogEntryTakesItsLikesAndCommentsWithIt()
    {
        var me = await database.CreateUserAsync("me");
        var climbId = await database.CreateClimbAsync();
        var entryId = await database.CreateLogEntryAsync(me, climbId);

        await database.CreateSocialService().LikeAsync(me, entryId);
        await database.CreateSocialService()
            .AddLogEntryCommentAsync(me, entryId, new CreateCommentDto { Content = "bye" });

        await using (var context = database.CreateContext())
        {
            context.LogEntries.Remove(await context.LogEntries.FirstAsync(l => l.Id == entryId));
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            Assert.Equal(0, await context.Likes.CountAsync());
            Assert.Equal(0, await context.Comments.CountAsync());
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HomeStatsSeparateSendsFromAttemptsAndRankGradesOnTheirOwnLadder()
    {
        var me = await database.CreateUserAsync("me");
        var easy = await database.CreateClimbAsync("Easy", GradeSystem.Yds, "5.9");
        var hard = await database.CreateClimbAsync("Hard", GradeSystem.Yds, "5.14a");
        var boulder = await database.CreateClimbAsync("Boulder", GradeSystem.VScale, "V5");

        await database.CreateLogEntryAsync(me, easy);
        await database.CreateLogEntryAsync(me, hard);
        await database.CreateLogEntryAsync(me, boulder, status: LogEntryStatus.Attempted);

        var stats = await database.CreateCatalogService().GetHomeStatsAsync(me);

        Assert.Equal(2, stats.Core.TotalSends);
        // Attempts counts every logged try, sends included, so the send rate reads off it.
        Assert.Equal(3, stats.Core.TotalAttempts);
        Assert.Equal(3, stats.Core.DistinctClimbs);
        Assert.Equal(0.667, stats.Core.SendRate, 3);

        // 5.14a beats 5.9 on the YDS ladder, which a naive numeric sort gets backwards.
        var yds = Assert.Single(stats.Core.HardestGrades, g => g.System == GradeSystem.Yds);
        Assert.Equal("5.14a", yds.Grade);

        // The attempted boulder never counts as a hardest send.
        Assert.DoesNotContain(stats.Core.HardestGrades, g => g.System == GradeSystem.VScale);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HomeStatsCountFollowedClimbersWhoLoggedSomethingThisWeek()
    {
        var me = await database.CreateUserAsync("me");
        var active = await database.CreateUserAsync("active");
        var quiet = await database.CreateUserAsync("quiet");
        var climbId = await database.CreateClimbAsync();

        var follows = database.CreateFollowService();
        await follows.FollowAsync(me, active);
        await follows.FollowAsync(me, quiet);

        await database.CreateLogEntryAsync(active, climbId, DateTime.UtcNow.AddDays(-1));
        await database.CreateLogEntryAsync(quiet, climbId, DateTime.UtcNow.AddDays(-30));

        var stats = await database.CreateCatalogService().GetHomeStatsAsync(me);

        Assert.Equal(2, stats.Social.FollowingCount);
        Assert.Equal(0, stats.Social.FollowerCount);
        Assert.Equal(1, stats.Social.FollowingActiveThisWeek);
    }
}
