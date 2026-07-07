using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ClimbId { get; set; }
    public Climb? Climb { get; set; }
}
