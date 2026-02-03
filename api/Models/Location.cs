using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Location
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(-90, 90)]
    public float? Latitude { get; set; }

    [Range(-180, 180)]
    public float? Longitude { get; set; }

    [StringLength(100)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    public ICollection<ClimbRoute> ClimbRoutes { get; set; } = new List<ClimbRoute>();
}
