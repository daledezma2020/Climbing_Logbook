using api.Models;
using api.Services;

namespace api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class SetterServiceIntegrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SetterCanBeCreatedUpdatedFoundAndDeleted()
    {
        await using var context = database.CreateContext();
        var service = new SetterService(context);

        var created = await service.CreateSetterAsync(new Setter { Name = "Alex" });
        Assert.Equal("Alex", (await service.GetSetterByIdAsync(created.Id))?.Name);

        var updated = await service.UpdateSetterAsync(created.Id, new Setter { Name = "Taylor" });
        Assert.Equal("Taylor", updated?.Name);
        Assert.True(await service.SetterExistsAsync(created.Id));

        Assert.True(await service.DeleteSetterAsync(created.Id));
        Assert.False(await service.DeleteSetterAsync(created.Id));
        Assert.Null(await service.UpdateSetterAsync(created.Id, new Setter { Name = "Missing" }));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SetterLookupAndListOrderingAreDeterministic()
    {
        await using var context = database.CreateContext();
        var service = new SetterService(context);
        await service.CreateSetterAsync(new Setter { Name = "Alex" });
        await service.CreateSetterAsync(new Setter { Name = "Zoe" });
        await service.CreateSetterAsync(new Setter { Name = "Blake" });

        Assert.Equal("Blake", (await service.GetSetterByNameAsync("Blake"))?.Name);
        Assert.Equal(["Zoe", "Blake", "Alex"], (await service.GetSettersAsync()).Select(x => x.Name));
        Assert.Null(await service.GetSetterByNameAsync("Missing"));
    }
}
