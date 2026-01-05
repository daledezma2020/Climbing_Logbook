using System.ComponentModel.DataAnnotations;

namespace api.DTO.ClimbRoute;

public class ClimbRouteDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Grade { get; set; } = string.Empty;

    [Range(0, 5)]
    public decimal AverageRating { get; set; }

    [StringLength(500)]
    public string? Picture { get; set; }

    [StringLength(500)]
    public string? Video { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;
}
