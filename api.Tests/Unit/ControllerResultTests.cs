using System.Reflection;
using api.Controllers;
using api.DTO;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Tests.Unit;

public class ControllerResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClimbLookupAndDeleteTranslateMissingRecordsToNotFound()
    {
        var service = new CatalogServiceStub { Climb = null, DeleteResult = false };
        var controller = new ClimbsController(service);

        Assert.IsType<NotFoundResult>((await controller.GetClimb(99)).Result);
        Assert.IsType<NotFoundResult>(await controller.DeleteClimb(99));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ManualClimbCreationReturnsItsLookupRoute()
    {
        var created = new ClimbSummaryDto { Id = 42, Name = "Created", Grade = "V3" };
        var controller = new ClimbsController(new CatalogServiceStub { Climb = created });

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

        var climbResult = (await new ClimbsController(service).ImportOpenBetaClimb("missing")).Result;
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
        public Task<List<LogEntryDto>> GetLogEntriesAsync() => Task.FromResult(new List<LogEntryDto>());
        public Task<LogEntryDto> CreateLogEntryAsync(CreateLogEntryDto dto) => Task.FromResult(new LogEntryDto { Id = 1, ClimbId = dto.ClimbId });
        public Task<SearchResponseDto> SearchAsync(string query, int limit) => Task.FromResult(new SearchResponseDto());
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
