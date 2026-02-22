using Faster.Modulith;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

Console.WriteLine($"[{DateTime.UtcNow:O}] Initializing the Generic Host.");

IHostBuilder builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    IConfiguration configuration = hostContext.Configuration;

    services.AddModulith(configuration, options =>
    {
        options.AddOffers();
        options.AddMembership();
        options.AddReports();
        options.AddPasses();
    });

});

using IHost host = builder.Build();

Console.WriteLine($"[{DateTime.UtcNow:O}] Host built. Starting application lifecycle.");

await host.RunAsync();

Console.WriteLine($"[{DateTime.UtcNow:O}] Application terminated gracefully.");

