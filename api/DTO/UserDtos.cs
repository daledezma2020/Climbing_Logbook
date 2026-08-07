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
