using api.Models;
using System.ComponentModel.DataAnnotations;

namespace api.DTO;

public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
}

public class CurrentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? PictureUrl { get; set; }
    public int? HomePlaceId { get; set; }
    public PlaceSummaryDto? HomePlace { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserProfileDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    // Only populated on the caller's own profile; null on the public one.
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public string? PictureUrl { get; set; }
    public int? HomePlaceId { get; set; }
    public PlaceSummaryDto? HomePlace { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserStatsDto Stats { get; set; } = new();
}

public class UserStatsDto
{
    public int TotalLogEntries { get; set; }
    public int DistinctClimbs { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = [];
    public Dictionary<string, int> ByDiscipline { get; set; } = [];
    public List<HardestGradeDto> HardestGrades { get; set; } = [];
    public List<PlaceVisitDto> TopPlaces { get; set; } = [];
}

public class HardestGradeDto
{
    public GradeSystem System { get; set; }
    public string Grade { get; set; } = string.Empty;
    public int ClimbId { get; set; }
    public string ClimbName { get; set; } = string.Empty;
}

public class PlaceVisitDto
{
    public PlaceSummaryDto Place { get; set; } = new();
    public int Count { get; set; }
}

public class UpdateUserProfileDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(500)]
    public string? PictureUrl { get; set; }

    public int? HomePlaceId { get; set; }
}
