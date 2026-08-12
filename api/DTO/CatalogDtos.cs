using api.Models;
using System.ComponentModel.DataAnnotations;

namespace api.DTO;

public record CoordinatesDto(double Latitude, double Longitude);

public class PlaceSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public PlaceKind Kind { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

public class BoardConfigurationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class ClimbSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClimbDiscipline Discipline { get; set; }
    public GradeSystem GradeSystem { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int? PlaceId { get; set; }
    public PlaceSummaryDto? Place { get; set; }
    public int? BoardConfigurationId { get; set; }
    public BoardConfigurationDto? BoardConfiguration { get; set; }
    public CustomLocationDto? CustomLocation { get; set; }
    public int? SetterId { get; set; }
    public string? SetterName { get; set; }
    public string? FirstAscentName { get; set; }
    public string? PictureUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal AverageRating { get; set; }
    public string[] Sources { get; set; } = [];
}

public class LogEntryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public UserSummaryDto? User { get; set; }
    public int ClimbId { get; set; }
    public ClimbSummaryDto? Climb { get; set; }
    public int? PlaceId { get; set; }
    public PlaceSummaryDto? Place { get; set; }
    public DateTime OccurredAt { get; set; }
    public LogEntryStatus Status { get; set; }
    public int? Rating { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByMe { get; set; }
}

public class CreateLogEntryDto
{
    [Required]
    public int ClimbId { get; set; }

    public int? PlaceId { get; set; }

    public DateTime? OccurredAt { get; set; }

    public LogEntryStatus Status { get; set; } = LogEntryStatus.Completed;

    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class CreateManualClimbDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ClimbDiscipline Discipline { get; set; } = ClimbDiscipline.Bouldering;

    public GradeSystem GradeSystem { get; set; } = GradeSystem.VScale;

    [Required]
    [StringLength(100)]
    public string Grade { get; set; } = string.Empty;

    public int? PlaceId { get; set; }

    public int? BoardConfigurationId { get; set; }

    [StringLength(100)]
    public string? BoardConfigurationName { get; set; }

    public int? SetterId { get; set; }

    [StringLength(100)]
    public string? SetterName { get; set; }

    [StringLength(100)]
    public string? FirstAscentName { get; set; }

    [StringLength(500)]
    public string? PictureUrl { get; set; }

    [StringLength(500)]
    public string? VideoUrl { get; set; }

    public CreateCustomLocationDto? CustomLocation { get; set; }
}

public class CustomLocationDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class CreateCustomLocationDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }
}

public class CreatePlaceDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public PlaceKind Kind { get; set; } = PlaceKind.Custom;

    public int? ParentPlaceId { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }
}

public class SearchResponseDto
{
    public List<SearchResultDto> Results { get; set; } = [];
    public Dictionary<string, string> Providers { get; set; } = new();
}

public class SearchResultDto
{
    public string Key { get; set; } = string.Empty;
    public string ResultType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string[] Sources { get; set; } = [];
    public GradeDto? Grade { get; set; }
    public ClimbDiscipline? Discipline { get; set; }
    public string? PlaceName { get; set; }
    public PlaceKind? PlaceKind { get; set; }
    public CoordinatesDto? Coordinates { get; set; }
    public int? LocalId { get; set; }
    public string? ExternalId { get; set; }
}

public record GradeDto(string System, string Value);
