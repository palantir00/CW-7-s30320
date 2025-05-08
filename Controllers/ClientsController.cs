namespace CW_7_s30320.Controllers;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;         
using CW_7_s30320.Models.DTOs;
using CW_7_s30320.Services;

[Route("api/clients")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly ITripsService _svc;
    public ClientsController(ITripsService svc) => _svc = svc;
    
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newId = await _svc.CreateClient(dto);
        return CreatedAtAction(nameof(GetClientTrips), new { id = newId }, new { IdClient = newId });
    }

    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await _svc.GetTripsForClient(id);
        if (trips == null) return NotFound($"Client {id} not found.");
        return Ok(trips);
    }

    [HttpPut("{id}/trips/{tripId}")]
    public async Task<IActionResult> Register(int id, int tripId)
    {
        var ok = await _svc.RegisterClientToTrip(id, tripId);
        return ok ? NoContent() : BadRequest("Cannot register (not found or full).");
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> Unregister(int id, int tripId)
    {
        var ok = await _svc.UnregisterClientFromTrip(id, tripId);
        return ok ? NoContent() : NotFound("Registration not found.");
    }
}