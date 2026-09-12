using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace OAuth.Developer.WebAPI.Data;

public class DeveloperProfile
{
    [Key]
    public string UserId { get; set; } = string.Empty;
    public bool IsDeveloperEnabled { get; set; }
    public string? OrganizationName { get; set; }
    public string? ContactEmail { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}

public class DeveloperDbContext(DbContextOptions<DeveloperDbContext> options) : DbContext(options)
{
    public DbSet<DeveloperProfile> DeveloperProfiles => Set<DeveloperProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DeveloperProfile>(entity =>
        {
            entity.ToTable("DeveloperProfiles");
            entity.HasKey(e => e.UserId);
        });
    }
}
