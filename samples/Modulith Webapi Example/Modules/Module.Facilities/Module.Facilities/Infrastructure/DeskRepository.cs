using System;
using System.Collections.Generic;
using System.Text;

// NOTICE: This namespace ends in '.Infrastructure'.
// This layer contains the "Plumbing". It deals with external systems like Databases, File Systems, or APIs.
// It relies on the Application layer telling it *what* to do, but it decides *how* to do it.
namespace Module.Facilities.Infrastructure;

/// <summary>
/// The "Librarian" for Desks. It handles the low-level details of saving desk assignments to the database.
/// <para>
/// <b>Why exists?</b> This follows the <i>Repository Pattern</i>. 
/// The Command Handler (Application Layer) shouldn't contain SQL queries (`INSERT INTO Desks...`).
/// Instead, it calls this class. This allows us to change the database (e.g., SQL -> CosmosDB) 
/// without breaking the business rules in the Handler.
/// </para>
/// </summary>
/// <remarks>
/// <b>Why Internal?</b> This class is an implementation detail of the Facilities module. 
/// No other module (Sales, HR) should ever talk to the Facilities database directly. 
/// They must go through the public API/UseCases.
/// </remarks>
internal class DeskRepository
{
    /// <summary>
    /// Persists the desk assignment.
    /// </summary>
    /// <param name="employeeId">The employee needing the desk.</param>
    /// <param name="floor">The specific location assigned.</param>
    /// <returns>A Task representing the async database operation.</returns>
    public async Task AssignDeskAsync(Guid employeeId, string floor)
    {
        // HOW: In a real application, you would inject an Entity Framework 'DbContext' or a Dapper 'IDbConnection'
        // and run the actual SQL here.
        // e.g., await _dbContext.Desks.AddAsync(new DeskEntity { EmployeeId = employeeId, Location = floor });

        // SIMULATION: We just print to the console to prove the data reached this layer.
        Console.WriteLine($"[Facilities DB] Assigned Desk on {floor} to Emp {employeeId}");

        // WHY Task.CompletedTask?
        // The method signature returns 'Task' (because database calls are usually Asynchronous/Slow).
        // However, this demo code runs Synchronously (instantly). 
        // We await 'Task.CompletedTask' to satisfy the compiler and simulate an async operation finishing immediately.
        await Task.CompletedTask;
    }
}