using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers.Api
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly SporSalonuDbContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentsApiController(SporSalonuDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /api/appointments/by-email?email=test@mail.com
        [HttpGet("by-email")]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email boş olamaz.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Ok(new List<object>());

            var list = await _context.Appointments
                .Include(a => a.Coach)
                .Include(a => a.TrainingProgram)
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    appointmentId = a.AppointmentId,
                    appointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm"),
                    coachName = a.Coach != null ? a.Coach.Name : "-",
                    trainingProgramName = a.TrainingProgram != null ? a.TrainingProgram.Name : "-",
                    status = a.Status.ToString()
                })
                .ToListAsync();

            return Ok(list);
        }
    }
}
