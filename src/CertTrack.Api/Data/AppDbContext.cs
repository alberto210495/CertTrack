using CertTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CertTrack.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserObjective> Objectives => Set<UserObjective>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(254);
            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserObjective>(objective =>
        {
            objective.Property(o => o.Title).HasMaxLength(200);
            objective.Property(o => o.Cert).HasMaxLength(30);

            // Stored as text ("Done") instead of a number, so the database is readable.
            objective.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);

            // SQLite does not store the time zone. Dates are saved in UTC, so mark them as UTC when read.
            objective.Property(o => o.LastReviewed).HasConversion(
                date => date,
                date => date.HasValue ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) : date);

            objective.HasOne<User>().WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
