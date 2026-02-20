using Faster.Modulith;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddModulith(null, options =>
{
    options.AddOffers();
    options.AddMembership();
    options.AddReports();
    options.AddPasses();
});

// 2. Build the Service Provider
var app = services.BuildServiceProvider();