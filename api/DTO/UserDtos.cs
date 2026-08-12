using api.Models;
using System.ComponentModel.DataAnnotations;

namespace api.DTO;

public class UserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PictureUrl { get; set; }
    public PlaceSummaryDto? HomePlace { get; set; }
    public int FollowerCount { get; set; }
    public bool IsFollowedByMe { get; set; }
    public bool IsMe { get; set; }
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
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowedByMe { get; set; }
    public bool IsMe { get; set; }
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

// Served from GET api/users/me/stats. Grouped so the home page can fetch every
// KPI in one call and render only the cards the user has turned on.
public class HomeStatsDto
{
    public CoreStatsDto Core { get; set; } = new();
    public ActivityStatsDto Activity { get; set; } = new();
    public PlacesStatsDto Places { get; set; } = new();
    public SocialStatsDto Social { get; set; } = new();
}

public class CoreStatsDto
{
    public int TotalSends { get; set; }
    public int TotalAttempts { get; set; }
    public int DistinctClimbs { get; set; }
    public double SendRate { get; set; }
    public List<HardestGradeDto> HardestGrades { get; set; } = [];
}

public class ActivityStatsDto
{
    public int SendsThisMonth { get; set; }
    public int DaysClimbedLast30 { get; set; }
    public int CurrentStreakWeeks { get; set; }
    public DateTime? LastClimbedAt { get; set; }
}

public class PlacesStatsDto
{
    public List<PlaceVisitDto> TopPlaces { get; set; } = [];
    public Dictionary<string, int> ByDiscipline { get; set; } = [];
}

public class SocialStatsDto
{
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int FollowingActiveThisWeek { get; set; }
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
