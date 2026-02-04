using Faster.Modulith;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrderingModule();
builder.Services.AddKitchenModule();
builder.Services.AddRoboticsModule();
builder.Services.AddModulith();

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// The API is just a "Post Office"
app.MapPost("/orders", async (IOrderingModule module) =>
{
    // We dispatch the ticket into the vault.
    // The CodeFix-generated Processor handles the rest.
    var result = await module.PlaceBurgerOrder("Big Burger", 11, string.Empty, CancellationToken.None)

    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
});

app.MapGet("/orders/{id}", async (Guid id, IOrderingModule module) =>
{
    // Even for queries, we use the dispatcher to keep the vault internal.
    var result = await module.PayOrder(id);
    return result.IsSuccess
        ? Results.Ok(result)
        : Results.NotFound(result.Error);

});

app.Run();
