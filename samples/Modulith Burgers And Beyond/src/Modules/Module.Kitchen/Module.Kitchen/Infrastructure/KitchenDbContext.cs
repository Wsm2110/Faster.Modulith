using Microsoft.EntityFrameworkCore;
using Module.Kitchen.Domain;

namespace Module.Kitchen.Infrastructure;

/// <summary>
/// The internal persistence vault for the Kitchen module.
/// This is hidden from all other modules.
/// </summary>
internal sealed class KitchenDbContext : DbContext
{
    public DbSet<KitchenTicket> Tickets => Set<KitchenTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Audit log for schema initialization [cite: 2026-01-29]
        Console.WriteLine($"[{DateTime.UtcNow}]: Initializing Kitchen Vault Schema.");

        var ticket = modelBuilder.Entity<KitchenTicket>();

        // Use the OrderId from the Event as the Primary Key for the ticket
        ticket.HasKey(x => x.OrderId);

        ticket.Property(x => x.TableNumber).IsRequired();
        ticket.Property(x => x.Description).HasMaxLength(500);
        ticket.Property(x => x.Status).HasConversion<string>();

        base.OnModelCreating(modelBuilder);
    }
}


