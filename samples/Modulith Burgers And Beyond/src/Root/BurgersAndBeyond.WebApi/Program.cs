using Faster.Modulith;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// --- Module Registrations ---
builder.Services.AddOrderingModule();
builder.Services.AddKitchenModule();
builder.Services.AddRoboticsModule();
builder.Services.AddModulith();

// --- Swagger Configuration ---
// Register the Swagger generator, defining 1 or more Swagger documents
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Faster Modulith API",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Faster Modulith API V1");
        c.RoutePrefix = string.Empty; // Serve the UI at the root
    });
}

app.UseHttpsRedirection();

// --- Post Office Endpoints ---

app.MapPost("/orders", async (IOrderingModule module) =>
{
    Console.WriteLine($"[{DateTime.UtcNow}] Dispatching Burger Order ticket to the vault.");

    // The CodeFix-generated Processor handles the rest.
    var result = await module.PlaceBurgerOrder("Big Burger", 11, string.Empty, CancellationToken.None);

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
});

app.MapGet("/orders/{id}", async (Guid id, IOrderingModule module) =>
{
    Console.WriteLine($"[{DateTime.UtcNow}] Querying vault for Order ID: {id}");

    // Even for queries, we use the dispatcher to keep the vault internal.
    var result = await module.PayOrder(id);

    return result.IsSuccess
        ? Results.Ok(result)
        : Results.NotFound(result.Error);
});

app.Run();