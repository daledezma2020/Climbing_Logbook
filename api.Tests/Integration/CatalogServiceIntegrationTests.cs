using api.DTO;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class CatalogServiceIntegrationTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UserCanCreateAndRetrieveAPlaceClimbAndLogEntry()
    {
        var catalog = database.CreateCatalogService();
        var place = await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "  Test Gym  ",
            Kind = PlaceKind.Gym,
            Latitude = 40.1,
            Longitude = -75.2
        });

        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "  Blue Arete  ",
            Grade = " V4 ",
            PlaceId = place.Id,
            SetterName = "  Alex  "
        });
        var occurredAt = new DateTime(2026, 7, 1, 18, 30, 0, DateTimeKind.Utc);
        var userId = await database.CreateUserAsync();
        var entry = await catalog.CreateLogEntryAsync(new CreateLogEntryDto
        {
            ClimbId = climb.Id,
            OccurredAt = occurredAt,
            Status = LogEntryStatus.Completed,
            Rating = 5,
            Notes = "Felt solid"
        }, userId);

        var retrieved = await catalog.GetClimbAsync(climb.Id);
        var logbook = await catalog.GetLogEntriesAsync();
        Assert.Equal("Test Gym", place.Name);
        Assert.Equal("Blue Arete", retrieved?.Name);
        Assert.Equal("V4", retrieved?.Grade);
        Assert.Equal("Alex", retrieved?.SetterName);
        Assert.Equal(5m, retrieved?.AverageRating);
        Assert.Equal(occurredAt, entry.OccurredAt);
        Assert.Equal("Blue Arete", Assert.Single(logbook).Climb?.Name);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManualClimbsRequireExactlyOneValidContext()
    {
        var catalog = database.CreateCatalogService();
        var place = await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Test Crag",
            Kind = PlaceKind.Outdoor,
            Latitude = 40,
            Longitude = -75
        });

        var none = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.CreateManualClimbAsync(new CreateManualClimbDto { Name = "No Context", Grade = "V1" }));
        var multiple = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.CreateManualClimbAsync(new CreateManualClimbDto
            {
                Name = "Two Contexts",
                Grade = "V2",
                PlaceId = place.Id,
                BoardConfigurationName = "Custom Board"
            }));
        var missingPlace = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.CreateManualClimbAsync(new CreateManualClimbDto { Name = "Missing", Grade = "V3", PlaceId = 9999 }));

        Assert.Contains("Exactly one", none.Message);
        Assert.Contains("Exactly one", multiple.Message);
        Assert.Equal("Place not found.", missingPlace.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task BoardAndCustomLocationClimbsPreserveTheirSelectedContext()
    {
        var catalog = database.CreateCatalogService();

        var boardClimb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Board Problem",
            Grade = "V7",
            BoardConfigurationName = "  Home Wall  "
        });
        var customClimb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Roadside Boulder",
            Grade = "V3",
            CustomLocation = new CreateCustomLocationDto
            {
                Name = "  Trail Pull-off  ",
                Latitude = 39.25,
                Longitude = -76.5
            }
        });

        Assert.Equal("Home Wall", boardClimb.BoardConfiguration?.Name);
        Assert.Null(boardClimb.Place);
        Assert.Equal("Trail Pull-off", customClimb.CustomLocation?.Name);
        Assert.Equal(39.25, customClimb.CustomLocation?.Latitude);
        Assert.Null(customClimb.BoardConfiguration);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ImportsAreIdempotentAndReuseAMatchingNearbyPlace()
    {
        var openBeta = new StubOpenBetaClient
        {
            Climb = new()
            {
                Uuid = "climb-1",
                Name = "Imported Boulder",
                Grade = "V5",
                ParentAreaUuid = "area-1",
                ParentAreaName = "Shared Crag",
                Latitude = 40.0000,
                Longitude = -75.0000
            }
        };
        var osm = new StubOsmClient
        {
            Place = new()
            {
                OsmType = "way",
                OsmId = "9",
                Name = "Shared Crag",
                Latitude = 40.0005,
                Longitude = -75.0005
            }
        };
        var catalog = database.CreateCatalogService(openBeta, osm);

        var first = await catalog.ImportOpenBetaClimbAsync("climb-1");
        var second = await catalog.ImportOpenBetaClimbAsync("climb-1");
        var importedPlace = await catalog.ImportOsmPlaceAsync("way", "9");

        Assert.NotNull(first);
        Assert.Equal(first.Id, second?.Id);
        Assert.Equal(first.PlaceId, importedPlace?.Id);
        Assert.Single(await catalog.GetClimbsAsync());
        Assert.Single(await catalog.GetPlacesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchReturnsLocalResultsWhenAnExternalProviderFails()
    {
        var openBeta = new StubOpenBetaClient { SearchException = new HttpRequestException("provider unavailable") };
        var catalog = database.CreateCatalogService(openBeta);
        var place = await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Local Crag",
            Kind = PlaceKind.Outdoor,
            Latitude = 40,
            Longitude = -75
        });
        await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Local Classic",
            Grade = "V2",
            PlaceId = place.Id
        });

        var response = await catalog.SearchAsync("classic", 100);

        var result = Assert.Single(response.Results);
        Assert.Equal("Local Classic", result.Name);
        Assert.Equal("complete", response.Providers["local"]);
        Assert.Equal("failed", response.Providers["openbeta"]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeletingAClimbRemovesItsLogEntries()
    {
        var catalog = database.CreateCatalogService();
        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Temporary",
            Grade = "V0",
            BoardConfigurationName = "Test Board"
        });
        await catalog.CreateLogEntryAsync(
            new CreateLogEntryDto { ClimbId = climb.Id, Rating = 3 },
            await database.CreateUserAsync());

        Assert.True(await catalog.DeleteClimbAsync(climb.Id));
        Assert.Empty(await catalog.GetLogEntriesAsync());
        Assert.False(await catalog.DeleteClimbAsync(climb.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExternalReferenceProviderAndIdAreUnique()
    {
        await using var context = database.CreateContext();
        var board = new BoardConfiguration { Name = "Test Board", Manufacturer = "Test", Year = 2026 };
        var first = new Climb { Name = "First", Grade = "V1", BoardConfiguration = board };
        var second = new Climb { Name = "Second", Grade = "V2", BoardConfiguration = board };
        context.AddRange(first, second);
        context.ClimbExternalReferences.AddRange(
            new ClimbExternalReference { Climb = first, Provider = ExternalProvider.OpenBeta, ExternalId = "duplicate" },
            new ClimbExternalReference { Climb = second, Provider = ExternalProvider.OpenBeta, ExternalId = "duplicate" });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CatalogListsAreOrderedAndLogEntriesDefaultToTheCurrentTime()
    {
        var catalog = database.CreateCatalogService();
        var place = await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Zeta Gym", Kind = PlaceKind.Gym, Latitude = 40, Longitude = -75
        });
        await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Alpha Crag", Kind = PlaceKind.Outdoor, Latitude = 41, Longitude = -76
        });
        var zeta = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Zeta Problem", Grade = "V1", PlaceId = place.Id
        });
        await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Alpha Problem", Grade = "V2", BoardConfigurationName = "Board"
        });
        var before = DateTime.UtcNow;
        var entry = await catalog.CreateLogEntryAsync(
            new CreateLogEntryDto { ClimbId = zeta.Id },
            await database.CreateUserAsync());
        var after = DateTime.UtcNow;

        Assert.Equal(["Alpha Crag", "Zeta Gym"], (await catalog.GetPlacesAsync()).Select(x => x.Name));
        Assert.Equal(["Alpha Problem", "Zeta Problem"], (await catalog.GetClimbsAsync()).Select(x => x.Name));
        Assert.InRange(entry.OccurredAt, before, after);
        Assert.Equal(LogEntryStatus.Completed, entry.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InvalidPlaceAndLogEntryReferencesAreRejected()
    {
        var catalog = database.CreateCatalogService();
        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Unmapped", Kind = PlaceKind.Custom
        }));
        var userId = await database.CreateUserAsync();
        var missingClimb = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.CreateLogEntryAsync(new CreateLogEntryDto { ClimbId = 999 }, userId));

        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Board Problem", Grade = "V1", BoardConfigurationName = "Board"
        });
        var missingPlace = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.CreateLogEntryAsync(new CreateLogEntryDto { ClimbId = climb.Id, PlaceId = 999 }, userId));

        Assert.Equal("Climb not found.", missingClimb.Message);
        Assert.Equal("Place not found.", missingPlace.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MissingOrIncompleteExternalRecordsAreNotImported()
    {
        var missingCatalog = database.CreateCatalogService(new StubOpenBetaClient(), new StubOsmClient());
        Assert.Null(await missingCatalog.ImportOpenBetaClimbAsync("missing"));
        Assert.Null(await missingCatalog.ImportOsmPlaceAsync("node", "1"));

        var incomplete = new StubOpenBetaClient
        {
            Climb = new() { Uuid = "incomplete", Name = "No Grade", Grade = null }
        };
        Assert.Null(await database.CreateCatalogService(incomplete).ImportOpenBetaClimbAsync("incomplete"));
        Assert.Empty(await missingCatalog.GetClimbsAsync());
        Assert.Empty(await missingCatalog.GetPlacesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SearchClampsProviderLimitAndDeduplicatesEquivalentKeys()
    {
        var openBeta = new StubOpenBetaClient();
        var catalog = database.CreateCatalogService(openBeta);
        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Duplicate Classic", Grade = "V4", BoardConfigurationName = "Board"
        });
        openBeta.SearchResults =
        [
            new SearchResultDto { Key = $"local:climb:{climb.Id}", ResultType = "climb", Name = "Duplicate Classic" }
        ];

        var response = await catalog.SearchAsync("duplicate", 100);

        Assert.Equal(25, openBeta.LastSearchLimit);
        Assert.Single(response.Results);
        Assert.Equal($"local:climb:{climb.Id}", response.Results[0].Key);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExistingBoardsAndSettersAreReusedByIdAndName()
    {
        var catalog = database.CreateCatalogService();
        var first = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "First", Grade = "V1", BoardConfigurationName = "Home Wall", SetterName = "Alex"
        });
        var byId = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Second", Grade = "V2", BoardConfigurationId = first.BoardConfigurationId, SetterId = first.SetterId
        });
        var byName = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Third", Grade = "V3", BoardConfigurationName = "home wall", SetterName = "Alex"
        });

        Assert.Equal(first.BoardConfigurationId, byId.BoardConfigurationId);
        Assert.Equal(first.SetterId, byId.SetterId);
        Assert.Equal(first.BoardConfigurationId, byName.BoardConfigurationId);
        Assert.Equal(first.SetterId, byName.SetterId);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.BoardConfigurations.CountAsync());
        Assert.Equal(1, await context.Setters.CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReferencedPlacesCannotBeDeleted()
    {
        var catalog = database.CreateCatalogService();
        var place = await catalog.CreatePlaceAsync(new CreatePlaceDto
        {
            Name = "Protected Crag", Kind = PlaceKind.Outdoor, Latitude = 40, Longitude = -75
        });
        await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Protected Climb", Grade = "V2", PlaceId = place.Id
        });
        await using var context = database.CreateContext();
        context.Places.Remove((await context.Places.FindAsync(place.Id))!);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReferencedSettersCannotBeDeleted()
    {
        var catalog = database.CreateCatalogService();
        var climb = await catalog.CreateManualClimbAsync(new CreateManualClimbDto
        {
            Name = "Protected Climb", Grade = "V2", BoardConfigurationName = "Board", SetterName = "Protected Setter"
        });
        await using var context = database.CreateContext();
        context.Setters.Remove((await context.Setters.FindAsync(climb.SetterId))!);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
