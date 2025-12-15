using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminAppointmentsController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public AdminAppointmentsController(SporSalonuDbContext context)
        {
            _context = context;
        }

        // GET: /AdminAppointments/Pending
        public async Task<IActionResult> Pending()
        {
            var list = await _context.Appointments
                .Include(a => a.Coach)
                .Include(a => a.TrainingProgram)
                .Where(a => a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var appt = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appt == null) return NotFound();

            // Aynı koç + aynı saat: Confirmed çakışması var mı?
            bool conflict = await _context.Appointments.AnyAsync(a =>
                a.AppointmentId != id &&
                a.CoachId == appt.CoachId &&
                a.AppointmentDate == appt.AppointmentDate &&
                a.Status == AppointmentStatus.Confirmed);

            if (conflict)
            {
                TempData["Error"] = "Bu saat için zaten onaylı bir randevu var.";
                return RedirectToAction(nameof(Pending));
            }

            appt.Status = AppointmentStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu onaylandı.";
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            appt.Status = AppointmentStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu reddedildi.";
            return RedirectToAction(nameof(Pending));
        }
    }
}
