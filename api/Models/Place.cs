using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Place
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public PlaceKind Kind { get; set; } = PlaceKind.Custom;

    public int? ParentPlaceId { get; set; }
    public Place? ParentPlace { get; set; }
    public ICollection<Place> Children { get; set; } = new List<Place>();

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

    public ICollection<Climb> Climbs { get; set; } = new List<Climb>();

    public ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    public ICollection<PlaceExternalReference> ExternalReferences { get; set; } = new List<PlaceExternalReference>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
