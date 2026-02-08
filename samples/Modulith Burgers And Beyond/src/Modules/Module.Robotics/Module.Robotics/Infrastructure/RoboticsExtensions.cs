using Microsoft.Extensions.DependencyInjection;
using Module.Robotics.Contracts;
using Module.Robotics.Infrastructure;

namespace Module.Robotics.Infrastructure;

public partial class RoboticsExtensions
{
    static partial void AddInfrastructure(IServiceCollection services)
    {
        services.AddScoped<IRobotHardware, SimulatorRobotHardware>();
        services.AddScoped<RoboticsDbContext>();
    }
}
