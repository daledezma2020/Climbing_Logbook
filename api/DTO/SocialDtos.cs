using System.ComponentModel.DataAnnotations;

namespace api.DTO;

public class CommentDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public UserSummaryDto? User { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ClimbId { get; set; }
    public int? LogEntryId { get; set; }
    public bool CanDelete { get; set; }
}

public class CreateCommentDto
{
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

