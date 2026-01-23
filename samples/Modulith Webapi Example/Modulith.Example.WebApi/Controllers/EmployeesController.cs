using Faster.Modulith;
using Faster.Modulith.Contracts;
using Microsoft.AspNetCore.Mvc;
using Module.HumanResources.Api; // Access to the HR Module's Public Interface

// NOTICE: This namespace is 'Host.Api'.
// This project acts as the "Shell" or "Host". It references all the Modules (Sales, HR, Facilities)
// and exposes them to the internet via HTTP.
namespace Host.Api.Controllers;

[ApiController]
[Route("api/employees")]
// HOW: Primary Constructor Injection.
// We inject 'IHumanResourcesApi'. The Controller knows NOTHING about Command Handlers, 
// Databases, or Domain Logic. It only knows the public methods exposed by the HR Module.
public class EmployeesController(IHumanResourcesModule Api) : ControllerBase
{
    // POST api/employees
    [HttpPost]
    [ProducesResponseType(typeof(HireResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HireEmployee([FromBody] HireRequest request)
    {
        // 1. THE BOUNDARY CROSSING
        // We are crossing from the "Web Layer" (Controllers) into the "Module Layer" (Business Logic).
        // This method call internally triggers the 'HireEmployeeHandler' we reviewed earlier.
        var result = await Api.HireEmployee(request.FirstName, request.LastName, request.DepartmentId);

        // 2. ERROR HANDLING
        // If the Domain Logic rejected the request (e.g. "Department is broke"),
        // we translate that Domain Error into an HTTP 400 Bad Request.
        if (!result.IsSuccess)
        {
            return BadRequest(new { Error = result.Error });
        }

        // 3. HTTP STATUS: 202 Accepted vs 201 Created
        // - 201 Created: "Done. Here is the resource."
        // - 202 Accepted: "We started working on it."
        //
        // WHY 202? As your comment notes, creating the employee row is just Step 1.
        // The *real* work (creating AD accounts, printing badges) happens in the background via Events.
        // Returning 202 tells the UI: "The ID is created, but the full onboarding isn't finished yet."
        return Accepted(new HireResponse(
            EmployeeId: result.Value,
            Status: "Hiring initiated. IT provisioning in progress..."
        ));
    }
}

// --- DTOs (Data Transfer Objects) ---
// These define the JSON shape that the Frontend (React/Angular) sends and receives.
public record HireRequest(string FirstName, string LastName, Guid DepartmentId);
public record HireResponse(Guid EmployeeId, string Status);