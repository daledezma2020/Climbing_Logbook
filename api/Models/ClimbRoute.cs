using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class ClimbRoute
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Grade { get; set; } = string.Empty;

    [Range(0, 5)]
    public decimal AverageRating { get; set; }

    [StringLength(100)]
    public string? Type { get; set; }

    [StringLength(500)]
    public string? Picture { get; set; }

    [StringLength(500)]
    public string? Video { get; set; }

    public int? LocationId { get; set; }
    public Location? Location { get; set; }

    public int? SetterId { get; set; }
    public Setter? Setter { get; set; }

    public List<Comment> Comments { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
