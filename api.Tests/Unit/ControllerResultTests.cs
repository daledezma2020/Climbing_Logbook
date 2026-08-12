using System.Reflection;
using api.Controllers;
using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace api.Tests.Unit;

public class ControllerResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClimbLookupAndDeleteTranslateMissingRecordsToNotFound()
    {
        var service = new CatalogServiceStub { Climb = null, DeleteResult = false };
        var controller = new ClimbsController(service, new UserServiceStub(), new SocialServiceStub());

        Assert.IsType<NotFoundResult>((await controller.GetClimb(99)).Result);
        Assert.IsType<NotFoundResult>(await controller.DeleteClimb(99));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ManualClimbCreationReturnsItsLookupRoute()
    {
        var created = new ClimbSummaryDto { Id = 42, Name = "Created", Grade = "V3" };
        var controller = new ClimbsController(new CatalogServiceStub { Climb = created }, new UserServiceStub(), new SocialServiceStub());

        var result = Assert.IsType<CreatedAtActionResult>(
            (await controller.CreateManualClimb(new CreateManualClimbDto { Name = "Created", Grade = "V3" })).Result);

        Assert.Equal(nameof(ClimbsController.GetClimb), result.ActionName);
        Assert.Equal(42, result.RouteValues?["id"]);
        Assert.Same(created, result.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FailedExternalImportsReturnActionableBadRequests()
    {
        var service = new CatalogServiceStub();

        var climbResult = (await new ClimbsController(service, new UserServiceStub(), new SocialServiceStub()).ImportOpenBetaClimb("missing")).Result;
        var placeResult = (await new PlacesController(service).ImportOsmPlace("node", "1")).Result;

        Assert.Equal("OpenBeta climb could not be imported.", Assert.IsType<BadRequestObjectResult>(climbResult).Value);
        Assert.Equal("OSM place could not be imported.", Assert.IsType<BadRequestObjectResult>(placeResult).Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void WriteEndpointsCarryAuthorizeMetadataWhileCatalogReadsRemainPublic()
    {
        Assert.NotNull(typeof(ClimbsController).GetMethod(nameof(ClimbsController.CreateManualClimb))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(ClimbsController).GetMethod(nameof(ClimbsController.DeleteClimb))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(LogEntriesController).GetMethod(nameof(LogEntriesController.CreateLogEntry))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(PlacesController).GetMethod(nameof(PlacesController.CreatePlace))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(ClimbsController).GetMethod(nameof(ClimbsController.GetClimbs))!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LogEntryCreationAttributesTheEntryToTheAuthenticatedUser()
    {
        var service = new CatalogServiceStub();
        var userService = new UserServiceStub { User = new AppUser { Id = 12, Username = "alex", DisplayName = "Alex" } };
        var controller = BuildLogEntriesController(service, userService, new SocialServiceStub(), authenticated: true);

        var result = Assert.IsType<CreatedAtActionResult>(
            (await controller.CreateLogEntry(new CreateLogEntryDto { ClimbId = 3 }, CancellationToken.None)).Result);

        Assert.Equal(12, service.LastCreatedForUserId);
        Assert.Equal(12, Assert.IsType<LogEntryDto>(result.Value).UserId);
        Assert.Equal(nameof(LogEntriesController.GetLogEntry), result.ActionName);
        Assert.Equal(1, result.RouteValues?["id"]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CreateLogEntryDtoCannotCarryAClientSuppliedOwner()
    {
        Assert.Null(typeof(CreateLogEntryDto).GetProperty("UserId"));
        Assert.Null(typeof(CreateLogEntryDto).GetProperty("User"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LogEntryReadsPassTheOptionalOwnerFilterThrough()
    {
        var service = new CatalogServiceStub();
        var controller = BuildLogEntriesController(
            service, new UserServiceStub(), new SocialServiceStub(), authenticated: false);

        await controller.GetLogEntries(null, CancellationToken.None);
        Assert.Null(service.LastRequestedUserId);

        await controller.GetLogEntries(9, CancellationToken.None);
        Assert.Equal(9, service.LastRequestedUserId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task LikingAMissingLogEntryIsNotFoundWhileASuccessfulLikeIsNoContent()
    {
        var users = new UserServiceStub { User = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" } };
        var social = new SocialServiceStub { TargetExists = false };
        var controller = BuildLogEntriesController(new CatalogServiceStub(), users, social, authenticated: true);

        Assert.IsType<NotFoundResult>(await controller.Like(99, CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Unlike(99, CancellationToken.None));

        social.TargetExists = true;
        Assert.IsType<NoContentResult>(await controller.Like(5, CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Unlike(5, CancellationToken.None));

        Assert.Equal((7, 5), social.LastLike);
        Assert.Equal((7, 5), social.LastUnlike);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommentingOnAMissingTargetIsNotFound()
    {
        var users = new UserServiceStub { User = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" } };
        var social = new SocialServiceStub { TargetExists = false };
        var dto = new CreateCommentDto { Content = "nice send" };

        var entries = BuildLogEntriesController(new CatalogServiceStub(), users, social, authenticated: true);
        Assert.IsType<NotFoundResult>((await entries.AddComment(99, dto, CancellationToken.None)).Result);

        var climbs = new ClimbsController(new CatalogServiceStub(), users, social)
        {
            ControllerContext = BuildContext(authenticated: true)
        };
        Assert.IsType<NotFoundResult>((await climbs.AddComment(99, dto, CancellationToken.None)).Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeletingSomeoneElsesCommentIsForbiddenAndAMissingOneIsNotFound()
    {
        var users = new UserServiceStub { User = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" } };
        var social = new SocialServiceStub();
        var controller = new CommentsController(users, social)
        {
            ControllerContext = BuildContext(authenticated: true)
        };

        social.DeletionResult = CommentDeletion.NotFound;
        Assert.IsType<NotFoundResult>(await controller.DeleteComment(1, CancellationToken.None));

        social.DeletionResult = CommentDeletion.Forbidden;
        Assert.IsType<ForbidResult>(await controller.DeleteComment(1, CancellationToken.None));

        social.DeletionResult = CommentDeletion.Deleted;
        Assert.IsType<NoContentResult>(await controller.DeleteComment(1, CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TheFeedIsBuiltForTheAuthenticatedCaller()
    {
        var users = new UserServiceStub { User = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" } };
        var social = new SocialServiceStub();
        var controller = new FeedController(users, social)
        {
            ControllerContext = BuildContext(authenticated: true)
        };

        Assert.IsType<OkObjectResult>((await controller.GetFeed(0, null, CancellationToken.None)).Result);
        Assert.Equal(7, social.LastFeedCallerId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void SocialWritesRequireAuthorizationWhileCommentReadsStayPublic()
    {
        Assert.NotNull(typeof(FeedController).GetMethod(nameof(FeedController.GetFeed))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(LogEntriesController).GetMethod(nameof(LogEntriesController.Like))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(LogEntriesController).GetMethod(nameof(LogEntriesController.Unlike))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(LogEntriesController).GetMethod(nameof(LogEntriesController.AddComment))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(ClimbsController).GetMethod(nameof(ClimbsController.AddComment))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(CommentsController).GetMethod(nameof(CommentsController.DeleteComment))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(UsersController).GetMethod(nameof(UsersController.GetMyStats))!
            .GetCustomAttribute<AuthorizeAttribute>());

        Assert.Null(typeof(LogEntriesController).GetMethod(nameof(LogEntriesController.GetComments))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(ClimbsController).GetMethod(nameof(ClimbsController.GetComments))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(ClimbsController).GetMethod(nameof(ClimbsController.GetClimbLogEntries))!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetterControllerTranslatesServiceOutcomes()
    {
        var service = new SetterServiceStub();
        var controller = new SettersController(service, NullLogger<SettersController>.Instance);

        Assert.IsType<NotFoundResult>((await controller.GetSetter(1)).Result);
        Assert.IsType<NotFoundResult>(await controller.DeleteSetter(1));

        service.Setter = new Setter { Id = 2, Name = "Alex" };
        service.DeleteResult = true;
        Assert.IsType<OkObjectResult>((await controller.GetSetter(2)).Result);
        Assert.IsType<NoContentResult>(await controller.DeleteSetter(2));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FollowingYourselfIsRejectedAsABadRequest()
    {
        var me = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" };
        var users = new UserServiceStub { User = me, FoundUser = me };
        var controller = BuildUsersController(users, new FollowServiceStub(), authenticated: true);

        var result = await controller.Follow("alex", CancellationToken.None);

        var problem = Assert.IsType<ProblemDetails>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FollowAndUnfollowReturnNoContentAndPairTheCallerWithTheTarget()
    {
        var users = new UserServiceStub
        {
            User = new AppUser { Id = 7, Username = "alex", DisplayName = "Alex" },
            FoundUser = new AppUser { Id = 9, Username = "sam", DisplayName = "Sam" }
        };
        var follows = new FollowServiceStub();
        var controller = BuildUsersController(users, follows, authenticated: true);

        Assert.IsType<NoContentResult>(await controller.Follow("sam", CancellationToken.None));
        Assert.IsType<NoContentResult>(await controller.Unfollow("sam", CancellationToken.None));

        Assert.Equal((7, 9), follows.LastFollow);
        Assert.Equal((7, 9), follows.LastUnfollow);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UnknownUsernamesAreNotFoundAcrossTheFollowAndConnectionRoutes()
    {
        var users = new UserServiceStub { FoundUser = null };
        var controller = BuildUsersController(users, new FollowServiceStub(), authenticated: true);

        Assert.IsType<NotFoundResult>(await controller.Follow("ghost", CancellationToken.None));
        Assert.IsType<NotFoundResult>(await controller.Unfollow("ghost", CancellationToken.None));
        Assert.IsType<NotFoundResult>((await controller.GetFollowers("ghost", 0, null, CancellationToken.None)).Result);
        Assert.IsType<NotFoundResult>((await controller.GetFollowing("ghost", 0, null, CancellationToken.None)).Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task VisitorsCanSearchAndBrowseConnectionsWithoutACallerIdentity()
    {
        var users = new UserServiceStub
        {
            FoundUser = new AppUser { Id = 9, Username = "sam", DisplayName = "Sam" },
            SearchResults = [new UserSummaryDto { Id = 9, Username = "sam", DisplayName = "Sam" }]
        };
        var follows = new FollowServiceStub();
        var controller = BuildUsersController(users, follows, authenticated: false);

        var search = Assert.IsType<OkObjectResult>((await controller.SearchUsers("sa", 10, CancellationToken.None)).Result);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<UserSummaryDto>>(search.Value));
        Assert.Null(users.LastSearchCallerId);

        Assert.IsType<OkObjectResult>((await controller.GetFollowers("sam", 0, null, CancellationToken.None)).Result);
        Assert.Null(follows.LastCallerId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FollowWritesRequireAuthorizationWhileSearchAndConnectionReadsStayPublic()
    {
        Assert.NotNull(typeof(UsersController).GetMethod(nameof(UsersController.Follow))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(typeof(UsersController).GetMethod(nameof(UsersController.Unfollow))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(UsersController).GetMethod(nameof(UsersController.SearchUsers))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(UsersController).GetMethod(nameof(UsersController.GetFollowers))!
            .GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(UsersController).GetMethod(nameof(UsersController.GetFollowing))!
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    private static ControllerContext BuildContext(bool authenticated)
    {
        var identity = authenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "auth0|caller")], "TestAuth")
            : new ClaimsIdentity();

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static UsersController BuildUsersController(
        UserServiceStub users,
        FollowServiceStub follows,
        bool authenticated)
    {
        return new UsersController(users, new CatalogServiceStub(), follows, new SocialServiceStub())
        {
            ControllerContext = BuildContext(authenticated)
        };
    }

    private static LogEntriesController BuildLogEntriesController(
        CatalogServiceStub catalog,
        UserServiceStub users,
        SocialServiceStub social,
        bool authenticated)
    {
        return new LogEntriesController(catalog, users, social)
        {
            ControllerContext = BuildContext(authenticated)
        };
    }

    private sealed class CatalogServiceStub : ICatalogService
    {
        public ClimbSummaryDto? Climb { get; set; }
        public PlaceSummaryDto? Place { get; set; }
        public bool DeleteResult { get; set; }

        public Task<List<ClimbSummaryDto>> GetClimbsAsync() => Task.FromResult(Climb == null ? new List<ClimbSummaryDto>() : new List<ClimbSummaryDto> { Climb });
        public Task<ClimbSummaryDto?> GetClimbAsync(int id) => Task.FromResult(Climb);
        public Task<ClimbSummaryDto> CreateManualClimbAsync(CreateManualClimbDto dto) => Task.FromResult(Climb!);
        public Task<ClimbSummaryDto?> ImportOpenBetaClimbAsync(string uuid) => Task.FromResult(Climb);
        public Task<bool> DeleteClimbAsync(int id) => Task.FromResult(DeleteResult);
        public Task<List<PlaceSummaryDto>> GetPlacesAsync() => Task.FromResult(Place == null ? new List<PlaceSummaryDto>() : new List<PlaceSummaryDto> { Place });
        public Task<PlaceSummaryDto> CreatePlaceAsync(CreatePlaceDto dto) => Task.FromResult(Place!);
        public Task<PlaceSummaryDto?> ImportOsmPlaceAsync(string osmType, string osmId) => Task.FromResult(Place);
        public Task<List<BoardConfigurationDto>> GetBoardConfigurationsAsync() => Task.FromResult(new List<BoardConfigurationDto>());
        public int? LastRequestedUserId { get; private set; }
        public int? LastCreatedForUserId { get; private set; }

        public Task<List<LogEntryDto>> GetLogEntriesAsync(int? userId = null)
        {
            LastRequestedUserId = userId;
            return Task.FromResult(new List<LogEntryDto>());
        }

        public Task<LogEntryDto> CreateLogEntryAsync(CreateLogEntryDto dto, int userId)
        {
            LastCreatedForUserId = userId;
            return Task.FromResult(new LogEntryDto { Id = 1, ClimbId = dto.ClimbId, UserId = userId });
        }
        public Task<SearchResponseDto> SearchAsync(string query, int limit) => Task.FromResult(new SearchResponseDto());

        public Task<PagedResult<LogEntryDto>> GetLogEntriesPagedAsync(int? userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            LastRequestedUserId = userId;
            return Task.FromResult(new PagedResult<LogEntryDto>([], 0, skip, take));
        }

        public Task<UserStatsDto> GetUserStatsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserStatsDto());

        public LogEntryDto? LogEntry { get; set; }
        public IReadOnlyCollection<int>? LastFeedUserIds { get; private set; }

        public Task<LogEntryDto?> GetLogEntryAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(LogEntry);

        public Task<PagedResult<LogEntryDto>> GetLogEntriesForUsersPagedAsync(
            IReadOnlyCollection<int> userIds, int skip, int take, CancellationToken cancellationToken = default)
        {
            LastFeedUserIds = userIds;
            return Task.FromResult(new PagedResult<LogEntryDto>([], 0, skip, take));
        }

        public Task<PagedResult<LogEntryDto>> GetClimbLogEntriesPagedAsync(
            int climbId, int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedResult<LogEntryDto>([], 0, skip, take));

        public Task<HomeStatsDto> GetHomeStatsAsync(int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new HomeStatsDto());
    }

    private sealed class SetterServiceStub : ISetterService
    {
        public Setter? Setter { get; set; }
        public bool DeleteResult { get; set; }

        public Task<IEnumerable<Setter>> GetSettersAsync() => Task.FromResult<IEnumerable<Setter>>(Setter == null ? [] : [Setter]);
        public Task<Setter?> GetSetterByIdAsync(int id) => Task.FromResult(Setter);
        public Task<Setter?> GetSetterByNameAsync(string name) => Task.FromResult(Setter);
        public Task<Setter> CreateSetterAsync(Setter setter) => Task.FromResult(setter);
        public Task<Setter?> UpdateSetterAsync(int id, Setter setter) => Task.FromResult(Setter);
        public Task<bool> DeleteSetterAsync(int id) => Task.FromResult(DeleteResult);
        public Task<bool> SetterExistsAsync(int id) => Task.FromResult(Setter != null);
    }
}
