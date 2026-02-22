using System;
using System.Collections.Generic;
using System.Text;

namespace Module.Robotics.Contracts;

/// <summary>
/// Internal interface for physical robot control. 
/// Hidden from the rest of the monolith.
/// </summary>
internal interface IRobotHardware
{
    ValueTask<int> GetBatteryLevel(CancellationToken ct);
    ValueTask PickupFromCounter(CancellationToken ct);
    ValueTask NavigateToTable(int tableNumber, CancellationToken ct);
    ValueTask<bool> ScanForDishes(CancellationToken ct);
    ValueTask PickupDirtyDishes(CancellationToken ct);
    ValueTask ReturnToBase(CancellationToken ct);
}
