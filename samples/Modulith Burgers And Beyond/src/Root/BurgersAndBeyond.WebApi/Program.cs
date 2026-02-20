using Faster.Modulith;
using Microsoft.OpenApi;
using Module.Ordering;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddModulith(builder.Configuration, options =>
{
    options.AddKitchen(kitchenOptions =>
    {
        kitchenOptions.UseInMemory = true;
    });

    options.AddOrdering(orderingOptions =>
    {
        orderingOptions.UseInMemory = true; 
    });

    options.AddRobotics();
    options.AddFeedback();
});

// --- Swagger Configuration ---
// Register the Swagger generator, defining 1 or more Swagger documents
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BurgersAndBeyond API",
        Version = "v1"
    });
});

var app = builder.Build();

// --- HTTP Request Pipeline ---
if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated Swagger as a JSON endpoint.
    app.UseSwagger();

    // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.)
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BurgersAndBeyond API");
        c.RoutePrefix = string.Empty; // Serve the UI at the root
    });
}

app.UseHttpsRedirection();
app.MapOrderingEndpoints();

app.Run();