namespace HotelChannelManager.Controllers;

using HotelChannelManager.Data;
using HotelChannelManager.Models;
using HotelChannelManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
private readonly AppDbContext _context;
private readonly ReservationQueue _queue;

public ReservationsController(AppDbContext context, ReservationQueue queue)
{
    _context = context;
    _queue = queue;
}

    [HttpGet]
public async Task<IActionResult> GetAll()
{
    var list = await _context.Reservations.ToListAsync();
    return Ok(new { success = true, count = list.Count, data = list });
}

[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var reservation = await _context.Reservations.FindAsync(id);
    if (reservation is null)
        return NotFound(new { success = false, error = "Reservation not found", id });

    return Ok(new { success = true, data = reservation });
}

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateReservationRequest request)
{
    if (string.IsNullOrWhiteSpace(request.GuestName))
        return BadRequest(new { success = false, error = "GuestName is required" });

    if (request.CheckOut <= request.CheckIn)
        return BadRequest(new { success = false, error = "CheckOut must be after CheckIn" });

    var reservation = new Reservation
    {
        GuestName = request.GuestName,
        RoomType = request.RoomType,
        CheckIn = request.CheckIn,
        CheckOut = request.CheckOut,
        Source = request.Source
    };

_context.Reservations.Add(reservation);
await _context.SaveChangesAsync();

// Kuyruğa ekle → Worker işleyecek
_queue.Enqueue(reservation);

return CreatedAtAction(nameof(GetById), new { id = reservation.Id },
    new { success = true, data = reservation });
}

[HttpPatch("{id}/status")]
public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
{
    var valid = new[] { "PENDING", "CONFIRMED", "FAILED", "CANCELLED" };
    if (!valid.Contains(request.Status))
        return BadRequest(new { success = false, error = "Invalid status", valid });

    var reservation = await _context.Reservations.FindAsync(id);
    if (reservation is null)
        return NotFound(new { success = false, error = "Reservation not found" });

    reservation.Status = request.Status;
    if (request.ErrorMessage is not null)
        reservation.ErrorMessage = request.ErrorMessage;

    await _context.SaveChangesAsync();

    return Ok(new { success = true, data = reservation });
}
}
public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}