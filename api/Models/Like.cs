using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Like
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    public int LogEntryId { get; set; }
    public LogEntry? LogEntry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
