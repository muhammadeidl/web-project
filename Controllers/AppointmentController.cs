using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly SporSalonuDbContext _context;
        private readonly UserManager<User> _userManager;

        public AppointmentController(SporSalonuDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        //  ADMIN LIST - All appointments
        // ============================================================
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        // ============================================================
        //  ADMIN LIST - Pending approvals only
        // ============================================================
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PendingApprovals()
        {
            var list = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .Where(a => a.Status == AppointmentStatus.Pending)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(list);
        }

        // ============================================================
        //  ADMIN APPROVE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var appt = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appt == null) return NotFound();

            if (appt.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "Bu randevu zaten işlem görmüş.";
                return RedirectToAction(nameof(PendingApprovals));
            }

            //  Overlap check (Confirmed ile çakışıyor mu?)
            int duration = appt.TrainingProgram?.Duration ?? 0;
            DateTime start = appt.AppointmentDate;
            DateTime end = start.AddMinutes(duration);

            bool conflict = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .AnyAsync(a =>
                    a.AppointmentId != appt.AppointmentId &&
                    a.CoachId == appt.CoachId &&
                    a.Status == AppointmentStatus.Confirmed &&
                    a.AppointmentDate.Date == start.Date &&
                    (
                        start < a.AppointmentDate.AddMinutes(a.TrainingProgram.Duration) &&
                        end > a.AppointmentDate
                    )
                );

            if (conflict)
            {
                TempData["Error"] = "Bu saat için zaten onaylı bir randevu var.";
                return RedirectToAction(nameof(PendingApprovals));
            }

            appt.Status = AppointmentStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu onaylandı.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        // ============================================================
        //  ADMIN REJECT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Reject(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            if (appt.Status != AppointmentStatus.Pending)
            {
                TempData["Error"] = "Bu randevu zaten işlem görmüş.";
                return RedirectToAction(nameof(PendingApprovals));
            }

            appt.Status = AppointmentStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu reddedildi.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        // ============================================================
        //  EDIT (GET) - Admin & Owner
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (!User.IsInRole("admin") && appointment.UserId != userId)
                return RedirectToAction("AccessDenied", "User");

            ViewBag.TrainingPrograms = new SelectList(_context.TrainingPrograms, "TrainingProgramId", "Name", appointment.TrainingProgramId);
            ViewBag.Coaches = new SelectList(_context.Coaches, "CoachId", "Name", appointment.CoachId);

            return View(appointment);
        }

        // ============================================================
        //  EDIT (POST) - Admin & Owner
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            var userId = _userManager.GetUserId(User);

            var existing = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (existing == null) return NotFound();

            if (!User.IsInRole("admin") && existing.UserId != userId)
                return RedirectToAction("AccessDenied", "User");

            // UserId sabit kalsın
            appointment.UserId = existing.UserId;

            // Admin değilse status’u değiştiremesin
            if (!User.IsInRole("admin"))
                appointment.Status = existing.Status;

            if (ModelState.IsValid)
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();

                return User.IsInRole("admin")
                    ? RedirectToAction(nameof(Index))
                    : RedirectToAction("Dashboard", "User");
            }

            ViewBag.TrainingPrograms = new SelectList(_context.TrainingPrograms, "TrainingProgramId", "Name", appointment.TrainingProgramId);
            ViewBag.Coaches = new SelectList(_context.Coaches, "CoachId", "Name", appointment.CoachId);
            return View(appointment);
        }

        // ============================================================
        //  DELETE (GET)
        // ============================================================
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            var appointment = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null) return NotFound();

            if (appointment.UserId != userId && !User.IsInRole("admin"))
                return RedirectToAction("Dashboard", "User");

            return View(appointment);
        }

        // ============================================================
        //  DELETE (POST)
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null) return NotFound();

            if (appointment.UserId != userId && !User.IsInRole("admin"))
                return RedirectToAction("Dashboard", "User");

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return User.IsInRole("admin")
                ? RedirectToAction(nameof(Index))
                : RedirectToAction("Dashboard", "User");
        }

        // ============================================================
        //  CREATE (GET)
        // ============================================================
        public IActionResult Create()
        {
            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name");
            return View();
        }

        // ============================================================
        //  CREATE (POST) -  Pending approval
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int gymId, int trainingprogramId, int coachId, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "User");

            DateTime finalDateTime = appointmentDate.Date.Add(appointmentTime);

            if (!IsTimeSlotAvailable(coachId, finalDateTime, trainingprogramId))
            {
                TempData["Error"] = "Bu saat uygun değil veya daha önce talep edilmiş.";
                ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name");
                return View();
            }

            var newAppointment = new Appointment
            {
                UserId = userId,
                TrainingProgramId = trainingprogramId,
                CoachId = coachId,
                AppointmentDate = finalDateTime,
                Status = AppointmentStatus.Pending // önemli
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu talebiniz alındı. Admin onayı bekleniyor.";
            return RedirectToAction("Dashboard", "User");
        }

        // ============================================================
        //  HELPER: Availability check (Pending + Confirmed bloklar)
        // ============================================================
        private bool IsTimeSlotAvailable(int coachId, DateTime appointmentDate, int trainingprogramId)
        {
            var program = _context.TrainingPrograms.Find(trainingprogramId);
            if (program == null) return false;

            int duration = program.Duration;
            DateTime end = appointmentDate.AddMinutes(duration);

            var existing = _context.Appointments
                .Include(a => a.TrainingProgram)
                .Where(a => a.CoachId == coachId &&
                            a.AppointmentDate.Date == appointmentDate.Date &&
                            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                .ToList();

            bool hasConflict = existing.Any(a =>
            {
                var existingStart = a.AppointmentDate;
                var existingEnd = existingStart.AddMinutes(a.TrainingProgram.Duration);
                return (appointmentDate < existingEnd && end > existingStart);
            });

            return !hasConflict;
        }

        // ============================================================
        //  API ENDPOINTS (AJAX)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetTrainingProgramsByGym(int gymId)
        {
            var list = await _context.TrainingPrograms
                .Where(s => s.GymId == gymId)
                .Select(s => new { s.TrainingProgramId, s.Name, s.Price, s.Duration })
                .ToListAsync();

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetCoachesByTrainingProgramAndGym(int gymId, int trainingprogramId)
        {
            var coaches = _context.Coaches
                .Where(e => e.GymId == gymId)
                .Select(e => new { e.CoachId, e.Name })
                .ToList();

            return Json(coaches);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int coachId, DateTime date, int trainingprogramId)
        {
            try
            {
                var program = await _context.TrainingPrograms.FindAsync(trainingprogramId);
                if (program == null) return BadRequest("Program not found");
                int duration = program.Duration;

                var dayOfWeek = (GymDay)date.DayOfWeek;

                var availability = await _context.CoachAvailability
                    .FirstOrDefaultAsync(c => c.CoachId == coachId && c.DayOfWeek == dayOfWeek);

                if (availability == null) return Json(new List<string>());

         //
                if (availability.IsClosed || !availability.StartTime.HasValue || !availability.EndTime.HasValue)
                    return Json(new List<string>());

                var existingAppointments = await _context.Appointments
                    .Include(a => a.TrainingProgram)
                    .Where(a => a.CoachId == coachId &&
                                a.AppointmentDate.Date == date.Date &&
                                (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
                    .ToListAsync();

                var availableSlots = new List<string>();

                TimeSpan currentSlot = availability.StartTime.Value;
                TimeSpan endTime = availability.EndTime.Value;


                while (currentSlot.Add(TimeSpan.FromMinutes(duration)) <= endTime)
                {
                    TimeSpan slotEnd = currentSlot.Add(TimeSpan.FromMinutes(duration));

                    bool isTaken = existingAppointments.Any(app =>
                    {
                        var appStart = app.AppointmentDate.TimeOfDay;
                        var appEnd = appStart.Add(TimeSpan.FromMinutes(app.TrainingProgram.Duration));
                        return (currentSlot < appEnd && slotEnd > appStart);
                    });

                    if (!isTaken)
                        availableSlots.Add(currentSlot.ToString(@"hh\:mm"));

                    currentSlot = currentSlot.Add(TimeSpan.FromMinutes(duration));
                }

                return Json(availableSlots);
            }
            catch
            {
                return Json(new List<string>());
            }
        }
    }
}
