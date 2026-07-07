using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Climb
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ClimbDiscipline Discipline { get; set; } = ClimbDiscipline.Bouldering;

    public GradeSystem GradeSystem { get; set; } = GradeSystem.VScale;

    [Required]
    [StringLength(100)]
    public string Grade { get; set; } = string.Empty;

    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    public int? BoardConfigurationId { get; set; }
    public BoardConfiguration? BoardConfiguration { get; set; }

    [StringLength(100)]
    public string? CustomLocationName { get; set; }

    [Range(-90, 90)]
    public double? CustomLocationLatitude { get; set; }

    [Range(-180, 180)]
    public double? CustomLocationLongitude { get; set; }

    public int? SetterId { get; set; }
    public Setter? Setter { get; set; }

    [StringLength(100)]
    public string? FirstAscentName { get; set; }

    [StringLength(500)]
    public string? PictureUrl { get; set; }

    [StringLength(500)]
    public string? VideoUrl { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    public ICollection<ClimbExternalReference> ExternalReferences { get; set; } = new List<ClimbExternalReference>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
