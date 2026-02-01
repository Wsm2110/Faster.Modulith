using Faster.Modulith;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register the core restaurant modules
//builder.Services.add(opt => opt.UseSqlServer("..."));
//builder.Services.AddKitchenModule(opt => opt.UseNpgsql("..."));
//builder.Services.AddLoyaltyModule(opt => opt.UseSqlite("..."));

//// Register the new logistics modules [cite: 2026-01-08]
//builder.Services.AddInventoryModule(opt => opt.UseSqlServer("..."));
//builder.Services.AddMarketingModule();
//builder.Services.AddEmployeeModule(opt => opt.UseNpgsql("..."));

var app = builder.Build();

// Standardized audit log for restaurant opening [cite: 2026-01-29]
Console.WriteLine($"[{DateTime.UtcNow}]: Burgers & Beyond Enterprise system initialized with 6 modules.");

app.Run();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}




app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
