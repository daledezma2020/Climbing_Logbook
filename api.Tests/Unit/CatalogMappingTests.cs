using api.Models;
using api.Services;

namespace api.Tests.Unit;

public class CatalogMappingTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void LogEntryMappingExposesItsAuthorSummary()
    {
        var entry = new LogEntry
        {
            Id = 3,
            ClimbId = 9,
            UserId = 5,
            User = new AppUser
            {
                Id = 5,
                Username = "alex",
                DisplayName = "Alex Climber",
                PictureUrl = "https://example.test/alex.png",
                Email = "alex@example.test",
                Auth0Subject = "auth0|123"
            }
        };

        var result = CatalogMapping.ToDto(entry);

        Assert.Equal(5, result.UserId);
        Assert.NotNull(result.User);
        Assert.Equal("alex", result.User.Username);
        Assert.Equal("Alex Climber", result.User.DisplayName);
        Assert.Equal("https://example.test/alex.png", result.User.PictureUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LogEntryMappingToleratesAnUnloadedAuthor()
    {
        var result = CatalogMapping.ToDto(new LogEntry { Id = 3, UserId = 5, User = null });

        Assert.Equal(5, result.UserId);
        Assert.Null(result.User);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ClimbMappingExposesContextRatingAndDistinctSources()
    {
        var climb = new Climb
        {
            Id = 42,
            Name = "The Test Piece",
            Grade = "V5",
            GradeSystem = GradeSystem.VScale,
            Discipline = ClimbDiscipline.Bouldering,
            CustomLocationName = "Hidden Boulder",
            CustomLocationLatitude = 40.125,
            CustomLocationLongitude = -75.25,
            Setter = new Setter { Id = 7, Name = "Alex" },
            SetterId = 7,
            LogEntries =
            [
                new LogEntry { Rating = 3 },
                new LogEntry { Rating = 5 },
                new LogEntry { Rating = null }
            ],
            ExternalReferences =
            [
                new ClimbExternalReference { Provider = ExternalProvider.OpenBeta },
                new ClimbExternalReference { Provider = ExternalProvider.OpenBeta },
                new ClimbExternalReference { Provider = ExternalProvider.MoonBoardSeed }
            ]
        };

        var result = CatalogMapping.ToDto(climb);

        Assert.Equal(42, result.Id);
        Assert.Equal("The Test Piece", result.Name);
        Assert.Equal(4m, result.AverageRating);
        Assert.Equal("Alex", result.SetterName);
        Assert.NotNull(result.CustomLocation);
        Assert.Equal("Hidden Boulder", result.CustomLocation.Name);
        Assert.Equal(["OpenBeta", "MoonBoardSeed"], result.Sources);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ClimbMappingOmitsAnIncompleteCustomLocation()
    {
        var climb = new Climb
        {
            Name = "Incomplete",
            Grade = "V1",
            CustomLocationName = "Somewhere",
            CustomLocationLatitude = 40
        };

        var result = CatalogMapping.ToDto(climb);

        Assert.Null(result.CustomLocation);
        Assert.Equal(0m, result.AverageRating);
    }

    [Theory]
    [InlineData("  mixed Case  ", "MIXED CASE")]
    [InlineData("Crag", "CRAG")]
    [Trait("Category", "Unit")]
    public void NormalizeIsTrimmedAndCaseInsensitive(string value, string expected)
    {
        Assert.Equal(expected, CatalogMapping.Normalize(value));
    }
}
