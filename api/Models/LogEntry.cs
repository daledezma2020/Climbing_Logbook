using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class LogEntry
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int ClimbId { get; set; }
    public Climb? Climb { get; set; }

    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public LogEntryStatus Status { get; set; } = LogEntryStatus.Completed;

    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Like> Likes { get; set; } = new List<Like>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
