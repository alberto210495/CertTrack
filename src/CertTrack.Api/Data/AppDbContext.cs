using CertTrack.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CertTrack.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.Property(u => u.Email).HasMaxLength(254);
            user.HasIndex(u => u.Email).IsUnique();
        });
    }
}
