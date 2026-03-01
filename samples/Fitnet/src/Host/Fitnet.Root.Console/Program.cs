using Faster.Modulith;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Module.Membership.Api;

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
    });  

});

using IHost host = builder.Build();

// call the API to ensure all services are properly registered and initialized, and to trigger any potential initialization issues early in the application lifecycle.
var membershipApi = host.Services.GetRequiredService<IMembershipApi>(); // Resolve the API to ensure all services are properly registered and initialized.
await membershipApi.PrepareMembership(Guid.NewGuid(), "Premium", "Special"); // Test the API to trigger any potential initialization issues.

Console.WriteLine($"[{DateTime.UtcNow:O}] Host built. Starting application lifecycle.");

await host.RunAsync();

Console.WriteLine($"[{DateTime.UtcNow:O}] Application terminated gracefully.");

