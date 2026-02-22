using Module.Robotics.Contracts;

namespace Module.Robotics.Infrastructure;

internal sealed class SimulatorRobotHardware : IRobotHardware
{
    public ValueTask<int> GetBatteryLevel(CancellationToken ct) => ValueTask.FromResult(95);

    public async ValueTask PickupFromCounter(CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow}]: HARDWARE - Engaging lift arm...");
        await Task.Delay(500, ct);
    }

    public async ValueTask NavigateToTable(int tableNumber, CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow}]: HARDWARE - Pathfinding to Table {tableNumber}...");
        await Task.Delay(2000, ct); // Simulated travel time
    }

    public ValueTask<bool> ScanForDishes(CancellationToken ct) => ValueTask.FromResult(true);

    public async ValueTask PickupDirtyDishes(CancellationToken ct) => await Task.Delay(500, ct);

    public async ValueTask ReturnToBase(CancellationToken ct)
    {
        Console.WriteLine($"[{DateTime.UtcNow}]: HARDWARE - Returning to Kitchen Dock.");
        await Task.Delay(1000, ct);
    }
}
