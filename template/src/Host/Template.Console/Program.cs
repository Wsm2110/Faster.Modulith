using Faster.Modulith;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var services = new ServiceCollection();

Console.WriteLine($"[{DateTime.UtcNow:O}] Initializing Generic Host via CreateDefaultBuilder...");

IHostBuilder builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    // The IConfiguration instance is accessed via the hostContext
    IConfiguration configuration = hostContext.Configuration;

    services.AddModulith(configuration, options =>
    {
        options.AddMySecondModule();
        // You can register your modules here using the options object
        // Example: options.AddMembership();
    });
});

using var host = builder.Build();

Console.WriteLine($"[{DateTime.UtcNow:O}] Application host built successfully.");

host.Run();