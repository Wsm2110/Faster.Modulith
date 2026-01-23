using Faster.Modulith;
using Microsoft.AspNetCore.Mvc;

// NOTICE: We are in the 'Host.Api' project.
// This is the public face of the Facilities module.
namespace Host.Api.Controllers;

[ApiController]
[Route("api/facilities")]
// HOW: Primary Constructor Injection.
// We inject the Facilities Public API interface.
public class FacilitiesController(IFacilitiesModule Api) : ControllerBase
{
    // POST api/facilities/guests
    // SCENARIO: A visitor is standing at the reception desk.
    // They need a badge NOW. They cannot "wait for an email" like a new hire.
    // Therefore, this endpoint is SYNCHRONOUS.
    [HttpPost("guests")]
    public async Task<IActionResult> GrantGuestAccess([FromBody] GuestReq req)
    {
        // ARCHITECTURE NOTE: Logic in Controller?
        // We are passing 'DateTime.UtcNow.AddHours(8)' here.
        // Is this business logic? Technically, yes.
        // Ideally, the UseCase should decide the default duration, or the Client should send it.
        // But for simple "Defaults", doing it in the controller is sometimes acceptable.
        var result = await Api.GrantGuestAccess(req.VisitorName, DateTime.UtcNow.AddHours(8));

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        // 3. HTTP STATUS: 200 OK
        // Contrast this with the HR Controller (which returned 202 Accepted).
        // Here, the operation is "Atomic" and "Immediate". 
        // When this line runs, the printer has already finished (simulated).
        // So we return the data (BadgeCode) immediately to the UI.
        return Ok(new
        {
            BadgeCode = result.Value
        });
    }
}

// Simple DTO for the JSON body
public record GuestReq(string VisitorName);