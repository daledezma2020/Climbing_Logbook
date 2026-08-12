using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(1000)]
    public string Content { get; set; } = string.Empty;

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Exactly one target is set, enforced by the CK_Comments_Target check constraint.
    public int? ClimbId { get; set; }
    public Climb? Climb { get; set; }

    public int? LogEntryId { get; set; }
    public LogEntry? LogEntry { get; set; }
}
