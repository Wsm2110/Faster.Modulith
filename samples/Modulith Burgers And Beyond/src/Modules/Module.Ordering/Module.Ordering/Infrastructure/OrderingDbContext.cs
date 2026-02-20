using Microsoft.EntityFrameworkCore;
using Module.Ordering.Domain;

namespace Module.Ordering.Infrastructure;

internal class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<BurgerOrder> Orders => Set<BurgerOrder>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<BurgerOrder>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Status).HasConversion<string>();

            builder.OwnsMany(x => x.Items, itemBuilder =>
            {
                // Define the Foreign Key back to BurgerOrder
                itemBuilder.WithOwner().HasForeignKey("OrderId");

                // Define the Primary Key for the Item itself.
                // If OrderItem has an 'Id' property, use:
                itemBuilder.HasKey("Id");

                // NOTE: If OrderItem is a pure Value Object with NO 'Id' property,
                // you must define a shadow key instead:
                // itemBuilder.Property<Guid>("Id");
                // itemBuilder.HasKey("Id");
            });

            // Map the private field for items [cite: 2026-01-28]
            builder.Metadata.FindNavigation(nameof(BurgerOrder.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}