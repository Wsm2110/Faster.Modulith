using Microsoft.EntityFrameworkCore;

namespace Module.Ordering.Domain;

internal class OrderingDbContext : DbContext
{
    public DbSet<BurgerOrder> Orders => Set<BurgerOrder>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<BurgerOrder>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasConversion<string>();

            // Map the private field for items [cite: 2026-01-28]
            builder.Metadata.FindNavigation(nameof(BurgerOrder.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}