using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Setter
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ClimbRoute> ClimbRoutes { get; set; } = new List<ClimbRoute>();
}
