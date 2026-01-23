using System;
using System.Collections.Generic;
using System.Text;

namespace Module.HumanResources.Infrastructure;

internal class EmployeeRepository
{
    // Real EF Core / Dapper code would go here
    public async Task SaveAsync(Guid id, string name)
        => Console.WriteLine($"[HR Repo] INSERT INTO Employees VALUES ({id}, '{name}')");

    public async Task UpdateEmailAsync(Guid id, string email)
        => Console.WriteLine($"[HR Repo] UPDATE Employees SET Email = '{email}' WHERE Id = {id}");

    internal async Task UpdateBadgeAsync(Guid employeeId, string badgeCode)
    {
        // 3. Update Badge (Used by OnBadgeIssued - Reaction to Facilities)
        Console.WriteLine($"[HR Repo] UPDATE Employees SET BadgeCode = '{badgeCode}' WHERE Id = {employeeId}");
        await Task.CompletedTask;
    }
}
