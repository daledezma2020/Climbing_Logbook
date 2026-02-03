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
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<Setter> Setters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ClimbRoute>()
            .HasMany(r => r.Comments)
            .WithOne(c => c.ClimbRoute)
            .HasForeignKey(c => c.ClimbRouteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClimbRoute>()
            .HasOne(r => r.Location)
            .WithMany(l => l.ClimbRoutes)
            .HasForeignKey(r => r.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClimbRoute>()
            .HasOne(r => r.Setter)
            .WithMany(s => s.ClimbRoutes)
            .HasForeignKey(r => r.SetterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}