using Microsoft.EntityFrameworkCore;
using Zamfara.Web.Models;

namespace Zamfara.Web.Data;

public sealed class ZamfaraDbContext : DbContext
{
    public ZamfaraDbContext(DbContextOptions<ZamfaraDbContext> options) : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();
    public DbSet<NewsPost> NewsPosts => Set<NewsPost>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
    public DbSet<FaqItem> FaqItems => Set<FaqItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<School>(e =>
        {
            e.HasIndex(s => s.Slug).IsUnique();
            e.Property(s => s.Slug).HasMaxLength(63);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.ShortName).HasMaxLength(60);
            e.Property(s => s.PrimaryColor).HasMaxLength(7);
            e.Property(s => s.AccentColor).HasMaxLength(7);
        });

        // A school owns its content; deleting a school deletes everything it owns.
        modelBuilder.Entity<NewsPost>(e =>
        {
            e.HasIndex(n => n.SchoolId);
            e.HasOne(n => n.School).WithMany(s => s.NewsPosts).HasForeignKey(n => n.SchoolId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CalendarEvent>(e =>
        {
            e.HasIndex(c => c.SchoolId);
            e.HasOne(c => c.School).WithMany(s => s.CalendarEvents).HasForeignKey(c => c.SchoolId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GalleryItem>(e =>
        {
            e.HasIndex(g => g.SchoolId);
            e.HasOne(g => g.School).WithMany(s => s.GalleryItems).HasForeignKey(g => g.SchoolId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FaqItem>(e =>
        {
            e.HasIndex(f => f.SchoolId);
            e.HasOne(f => f.School).WithMany(s => s.FaqItems).HasForeignKey(f => f.SchoolId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
