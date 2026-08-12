using System.ComponentModel.DataAnnotations;
using api.DTO;

namespace api.Tests.Unit;

public class DtoValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [Trait("Category", "Unit")]
    public void LogEntryRatingOutsideOneThroughFiveIsInvalid(int rating)
    {
        var dto = new CreateLogEntryDto { ClimbId = 1, Rating = rating };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), [], true));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ManualClimbRequiresANameAndGrade()
    {
        var dto = new CreateManualClimbDto();
        var results = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), results, true));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateManualClimbDto.Name)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(CreateManualClimbDto.Grade)));
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    [Trait("Category", "Unit")]
    public void CustomLocationRejectsCoordinatesOutsideWorldBounds(double latitude, double longitude)
    {
        var dto = new CreateCustomLocationDto { Name = "Test", Latitude = latitude, Longitude = longitude };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), [], true));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Category", "Unit")]
    public void CommentsRejectEmptyContent(string content)
    {
        var dto = new CreateCommentDto { Content = content };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), [], true));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommentsRejectContentBeyondOneThousandCharacters()
    {
        var dto = new CreateCommentDto { Content = new string('x', 1001) };
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), [], true));

        dto.Content = new string('x', 1000);
        Assert.True(Validator.TryValidateObject(dto, new ValidationContext(dto), [], true));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommentsCannotCarryAClientSuppliedAuthorOrTarget()
    {
        Assert.Null(typeof(CreateCommentDto).GetProperty("UserId"));
        Assert.Null(typeof(CreateCommentDto).GetProperty("ClimbId"));
        Assert.Null(typeof(CreateCommentDto).GetProperty("LogEntryId"));
    }
}
