using Microsoft.Extensions.DependencyInjection;
using Module.Sales.Api;
using Module.Shipping.Api;
using Faster.Modulith;
using Faster.Modulith.Contracts;

Console.WriteLine("Hello, World!");

var services = new ServiceCollection()
    .AddShippingModule()
    .AddSalesModule()
    .AddModulith();

// 2. Build the Service Provider
var app = services.BuildServiceProvider();

// 3. Resolve the IEventBus instance
var salesModule = app.GetRequiredService<ISalesModule>();
var Orchestrator = app.GetRequiredService<IOrchestrator>(); 

var orderId = Guid.NewGuid();
var customerId = Guid.NewGuid();
var productId = Guid.NewGuid();

Console.WriteLine($"[User] Placing Order {orderId}...");

// 2. EXECUTE USE CASE (Synchronous)
// This runs: SalesHandler -> Internal Commands -> Return
var result = await salesModule.PlaceOrder(orderId, productId, 10, customerId);
if (result.IsSuccess)
{
    Console.WriteLine($"[API] Order Placed Successfully! ID: {result.Value}");
}
else
{
    Console.WriteLine($"[API] Failed: {result.Error}");
    return;
}


// ---------------------------------------------------------
// 3. WAIT FOR ASYNC EVENTS (Fire-and-Forget)
// ---------------------------------------------------------
// The Orchestrator fired 'OrderPlacedEvent' in the background.
// We pause here to let the Shipping module (running on a background thread) finish its work.
Console.WriteLine("\n[System] Waiting for background processes (Shipping)...");
await Task.Delay(1000);

// ---------------------------------------------------------
// 4. QUERY STATUS (Cross-Module Verification)
// ---------------------------------------------------------
// API -> Shipping Module
Console.WriteLine("\n[User] Checking Shipment Status...");

var statusQuery = new GetShipmentStatusUseCase(orderId);
//var statusResult = await Orchestrator.Dispatch(statusQuery);

//Console.WriteLine($"[API] Shipment Status: {statusResult.Value}");

Console.WriteLine("\n=== Demo Complete ===");
Console.ReadKey();