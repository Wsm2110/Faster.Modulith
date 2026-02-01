using Microsoft.EntityFrameworkCore;

namespace Module.Ordering.Domain;

internal class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}