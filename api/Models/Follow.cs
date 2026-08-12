using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Follow
{
    [Key]
    public int Id { get; set; }

    public int FollowerId { get; set; }
    public AppUser? Follower { get; set; }

    public int FolloweeId { get; set; }
    public AppUser? Followee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
