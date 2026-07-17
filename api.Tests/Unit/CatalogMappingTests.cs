using api.Models;
using api.Services;

namespace api.Tests.Unit;

public class CatalogMappingTests
{
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
