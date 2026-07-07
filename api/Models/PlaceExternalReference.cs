using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class PlaceExternalReference
{
    [Key]
    public int Id { get; set; }

    public int PlaceId { get; set; }
    public Place? Place { get; set; }

    public ExternalProvider Provider { get; set; }

    [Required]
    [StringLength(200)]
    public string ExternalId { get; set; } = string.Empty;

    [StringLength(500)]
    public string? ExternalUrl { get; set; }

    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}
