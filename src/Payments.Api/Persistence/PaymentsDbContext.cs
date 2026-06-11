using Microsoft.EntityFrameworkCore;
using Payments.Api.Domain;

namespace Payments.Api.Persistence;

/// <summary>
/// Durable storage for deposit records (NFR-5). Backed by SQLite.
/// </summary>
public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Deposit> Deposits => Set<Deposit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Deposit>(entity =>
        {
            entity.HasKey(d => d.DepositId);
            entity.Property(d => d.Status).HasConversion<string>();
            entity.HasIndex(d => d.IdempotencyKey).IsUnique();
        });
    }
}
