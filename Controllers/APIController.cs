using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "VISITOR")]

    [Route("api/[controller]")]
    [ApiController]
    public class APIController : ControllerBase
    {
        private readonly SporSalonuDbContext _context;

        public APIController(SporSalonuDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var appointments = await _context.Appointments.ToListAsync();
            return Ok(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Appointment appointment)
        {
            if (appointment == null)
            {
                return BadRequest(new { message = "Invalid appointment data." });
            }

            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Appointment added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while adding the appointment.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound(new { message = "The appointment was not found." });
            }

            _context.Appointments.Remove(appointment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the appointment.", error = ex.Message });
            }

            return Ok(new { message = "The appointment has been successfully deleted." });
        }
    }
}
