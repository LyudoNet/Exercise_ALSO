namespace HierarchyAccounts.Infrastructure.Data;

using HierarchyAccounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(a => a.Depth)
                  .IsRequired();

            entity.Property(a => a.CreatedAt)
                  .IsRequired();

            // Self-referencing relationship: each account optionally belongs to a parent account
            entity.HasOne(a => a.Parent)
                  .WithMany(a => a.Children)
                  .HasForeignKey(a => a.ParentId)
                  // Restrict cascade delete — child reassignment is handled in application code
                  .OnDelete(DeleteBehavior.Restrict);

            // Index for fast lookup of all children of a given parent
            entity.HasIndex(a => a.ParentId);

            // Index for depth-based filtering
            entity.HasIndex(a => a.Depth);
        });
    }
}
