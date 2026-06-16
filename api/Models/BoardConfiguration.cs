using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class BoardConfiguration
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Climb> Climbs { get; set; } = new List<Climb>();
}
