using api.Models;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Climb> Climbs { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Place> Places { get; set; } = null!;
    public DbSet<BoardConfiguration> BoardConfigurations { get; set; } = null!;
    public DbSet<LogEntry> LogEntries { get; set; } = null!;
    public DbSet<ClimbExternalReference> ClimbExternalReferences { get; set; } = null!;
    public DbSet<PlaceExternalReference> PlaceExternalReferences { get; set; } = null!;
    public DbSet<Setter> Setters { get; set; } = null!;
    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<Follow> Follows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Climb>()
            .ToTable(t => t.HasCheckConstraint(
                    "CK_Climbs_Context",
                    """
                    (
                        "PlaceId" IS NOT NULL
                        AND "BoardConfigurationId" IS NULL
                        AND "CustomLocationName" IS NULL
                        AND "CustomLocationLatitude" IS NULL
                        AND "CustomLocationLongitude" IS NULL
                    )
                    OR (
                        "PlaceId" IS NULL
                        AND "BoardConfigurationId" IS NOT NULL
                        AND "CustomLocationName" IS NULL
                        AND "CustomLocationLatitude" IS NULL
                        AND "CustomLocationLongitude" IS NULL
                    )
                    OR (
                        "PlaceId" IS NULL
                        AND "BoardConfigurationId" IS NULL
                        AND "CustomLocationName" IS NOT NULL
                        AND "CustomLocationLatitude" IS NOT NULL
                        AND "CustomLocationLongitude" IS NOT NULL
                    )
                    """));

        modelBuilder.Entity<Climb>()
            .HasMany(c => c.Comments)
            .WithOne(c => c.Climb)
            .HasForeignKey(c => c.ClimbId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Climb>()
            .HasOne(c => c.Place)
            .WithMany(p => p.Climbs)
            .HasForeignKey(c => c.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Climb>()
            .HasOne(c => c.BoardConfiguration)
            .WithMany(b => b.Climbs)
            .HasForeignKey(c => c.BoardConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Climb>()
            .HasOne(c => c.Setter)
            .WithMany(s => s.Climbs)
            .HasForeignKey(c => c.SetterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Place>()
            .HasOne(p => p.ParentPlace)
            .WithMany(p => p.Children)
            .HasForeignKey(p => p.ParentPlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.HomePlace)
            .WithMany()
            .HasForeignKey(u => u.HomePlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Auth0Subject)
            .IsUnique();

        modelBuilder.Entity<Follow>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Follows_NoSelfFollow",
                """
                "FollowerId" <> "FolloweeId"
                """));

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Follow>()
            .HasOne(f => f.Followee)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FolloweeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Follow>()
            .HasIndex(f => new { f.FollowerId, f.FolloweeId })
            .IsUnique();

        modelBuilder.Entity<Follow>()
            .HasIndex(f => f.FolloweeId);

        modelBuilder.Entity<LogEntry>()
            .HasOne(l => l.User)
            .WithMany(u => u.LogEntries)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LogEntry>()
            .HasOne(l => l.Climb)
            .WithMany(c => c.LogEntries)
            .HasForeignKey(l => l.ClimbId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LogEntry>()
            .HasOne(l => l.Place)
            .WithMany(p => p.LogEntries)
            .HasForeignKey(l => l.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClimbExternalReference>()
            .HasOne(r => r.Climb)
            .WithMany(c => c.ExternalReferences)
            .HasForeignKey(r => r.ClimbId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClimbExternalReference>()
            .HasIndex(r => new { r.Provider, r.ExternalId })
            .IsUnique();

        modelBuilder.Entity<PlaceExternalReference>()
            .HasOne(r => r.Place)
            .WithMany(p => p.ExternalReferences)
            .HasForeignKey(r => r.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PlaceExternalReference>()
            .HasIndex(r => new { r.Provider, r.ExternalId })
            .IsUnique();
    }
}
