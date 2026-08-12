using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class AppUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string Auth0Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(1000)]
    public string? Bio { get; set; }

    [StringLength(500)]
    public string? PictureUrl { get; set; }

    public int? HomePlaceId { get; set; }
    public Place? HomePlace { get; set; }

    public ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Follow> Following { get; set; } = new List<Follow>();

    public ICollection<Follow> Followers { get; set; } = new List<Follow>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
