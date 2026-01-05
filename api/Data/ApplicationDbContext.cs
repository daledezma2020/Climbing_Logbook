using Microsoft.EntityFrameworkCore;
using api.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ClimbRoute> ClimbRoutes { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClimbRoute>()
            .HasMany(r => r.Comments)
            .WithOne(c => c.ClimbRoute)
            .HasForeignKey(c => c.ClimbRouteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}