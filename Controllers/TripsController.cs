namespace CW_7_s30320.Controllers;

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CW_7_s30320.Services;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public TripsController(ITripsService tripsService)
        => _tripsService = tripsService;

    [HttpGet]
    public async Task<IActionResult> GetTrips()
        => Ok(await _tripsService.GetTrips());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTrip(int id)
    {
        var all = await _tripsService.GetTrips();
        var trip = all.FirstOrDefault(t => t.Id == id);
        return trip == null ? NotFound() : Ok(trip);
    }
}