using Microsoft.EntityFrameworkCore;
using Module.Robotics.Domain;

namespace Module.Robotics.Infrastructure;


internal sealed class RoboticsDbContext : DbContext
{
    public DbSet<DeliveryTask> DeliveryTasks => Set<DeliveryTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<DeliveryTask>();

        task.HasKey(x => x.OrderId);
        task.Property(x => x.Status).HasConversion<string>();
        
        base.OnModelCreating(modelBuilder);
    }
}
