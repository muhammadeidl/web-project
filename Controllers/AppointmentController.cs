using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitnessCenter.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public AppointmentController(SporSalonuDbContext context)
        {
            _context = context;
        }

        // ------------------ إجراءات خاصة بالإدمن فقط ------------------

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .ToListAsync();

            return View(appointments);
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewBag.TrainingPrograms = new SelectList(_context.TrainingPrograms, "TrainingProgramId", "Name", appointment.TrainingProgramId);
            ViewBag.Coaches = new SelectList(_context.Coaches, "CoachId", "Name", appointment.CoachId);

            return View(appointment);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return NotFound();
            }

            // لا تنسى استعادة UserId للموعد إذا كان غير موجود في النموذج
            var existingAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.AppointmentId == id);
            if (existingAppointment != null)
            {
                appointment.UserId = existingAppointment.UserId;
            }


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(a => a.AppointmentId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.TrainingPrograms = new SelectList(_context.TrainingPrograms, "TrainingProgramId", "Name", appointment.TrainingProgramId);
            ViewBag.Coaches = new SelectList(_context.Coaches, "CoachId", "Name", appointment.CoachId);

            return View(appointment);
        }

        // ------------------ إجراءات يمكن للمستخدم العادي الوصول إليها (تتطلب التحقق من الملكية) ------------------

        public async Task<IActionResult> Delete(int id)
        {
            string currentUserId = User.FindFirstValue("UserId");

            var appointment = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            // **الإصلاح (1): التحقق الأمني وتحديد مسار التوجيه إلى UserController**
            if (appointment.UserId != currentUserId && !User.IsInRole("admin"))
            {
                return RedirectToAction("Dashboard", "User");
            }

            return View(appointment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string currentUserId = User.FindFirstValue("UserId");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                // **الإصلاح (2): التحقق الأمني وتحديد مسار التوجيه إلى UserController**
                if (appointment.UserId != currentUserId && !User.IsInRole("admin"))
                {
                    return RedirectToAction("Dashboard", "User");
                }

                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }

            if (User.IsInRole("admin"))
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction("Dashboard", "User");
            }
        }

        public IActionResult Create()
        {
            string userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Appointment") });
            }

            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment, int gymId, int trainingprogramId, int coachId, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            string userId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Appointment") });
            }

            appointmentDate = appointmentDate.Date.Add(appointmentTime);

            if (!IsTimeSlotAvailable(coachId, appointmentDate, trainingprogramId))
            {
                TempData["Error"] = "The selected time slot is already blocked or conflicts with another appointment.";
                return RedirectToAction(nameof(Create));
            }

            var newAppointment = new Appointment
            {
                UserId = userId,
                TrainingProgramId = trainingprogramId,
                CoachId = coachId,
                AppointmentDate = appointmentDate,
                Status = AppointmentStatus.Confirmed
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Dashboard", "User");
        }

        // **الإصلاح (3): تعديل منطق التحقق من التوافر لاستخدام كائنات DateTime الكاملة**
        private bool IsTimeSlotAvailable(int coachId, DateTime appointmentDate, int trainingprogramId)
        {
            var trainingprogram = _context.TrainingPrograms.Find(trainingprogramId);
            if (trainingprogram == null) return false;

            int duration = trainingprogram.Duration;
            DateTime newAppointmentEnd = appointmentDate.AddMinutes(duration);

            // 1. التحقق ضد الأوقات المحظورة (UnavailableSlots)
            bool blocked = _context.UnavailableSlots.Any(us =>
                us.CoachId == coachId &&
                !(newAppointmentEnd <= us.StartTime || appointmentDate >= us.EndTime));

            if (blocked) return false;

            // 2. التحقق ضد المواعيد الأخرى المحجوزة (Appointments)
            var existingAppointments = _context.Appointments
                .Include(a => a.TrainingProgram)
                .Where(a => a.CoachId == coachId &&
                            a.AppointmentDate.Date == appointmentDate.Date &&
                            a.Status != AppointmentStatus.Cancelled)
                .ToList();

            bool conflictWithExisting = existingAppointments.Any(a =>
            {
                DateTime existingStart = a.AppointmentDate;
                DateTime existingEnd = a.AppointmentDate.AddMinutes(a.TrainingProgram.Duration);

                // منطق التحقق من التداخل: لا يوجد تداخل إذا كانت النهاية الجديدة <= البداية الموجودة OR البداية الجديدة >= النهاية الموجودة
                return !(newAppointmentEnd <= existingStart || appointmentDate >= existingEnd);
            });

            return !conflictWithExisting;
        }

        // ------------------ إجراءات الـ API ------------------

        [HttpGet]
        public async Task<IActionResult> GetTrainingProgramsByGym(int gymId)
        {
            var trainingprograms = await _context.TrainingPrograms
                .Where(s => s.GymId == gymId)
                .Select(s => new {
                    trainingprogramId = s.TrainingProgramId,
                    name = s.Name,
                    price = s.Price,
                    duration = s.Duration
                })
                .ToListAsync();

            return Json(trainingprograms);
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
                date = date.Date;

                var trainingprogram = await _context.TrainingPrograms.FindAsync(trainingprogramId);
                if (trainingprogram == null) return BadRequest("Training Program not found");

                int duration = trainingprogram.Duration;

                var dayOfWeek = (Models.DayOfWeek)((int)date.DayOfWeek);

                var availability = await _context.CoachAvailability
                    .Where(a => a.CoachId == coachId && a.DayOfWeek == dayOfWeek)
                    .ToListAsync();

                if (!availability.Any())
                    return Json(new List<object>());

                var appointments = await _context.Appointments
                    .Include(a => a.TrainingProgram)
                    .Where(a =>
                        a.CoachId == coachId &&
                        a.AppointmentDate.Date == date.Date &&
                        a.Status != AppointmentStatus.Cancelled)
                    .ToListAsync();

                var booked = appointments.Select(a => new
                {
                    Start = a.AppointmentDate.TimeOfDay,
                    End = a.AppointmentDate.TimeOfDay + TimeSpan.FromMinutes(a.TrainingProgram.Duration)
                }).ToList();

                var result = new List<object>();

                foreach (var av in availability)
                {
                    var current = av.StartTime;

                    while (current.Add(TimeSpan.FromMinutes(duration)) <= av.EndTime)
                    {
                        var end = current.Add(TimeSpan.FromMinutes(duration));

                        bool conflict = booked.Any(b =>
                            !(end <= b.Start || current >= b.End));

                        if (!conflict)
                        {
                            result.Add(new
                            {
                                time = current.ToString(@"hh\:mm"),
                                isAvailable = true
                            });
                        }

                        current = current.Add(TimeSpan.FromMinutes(30));
                    }
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Json(new List<object>());
            }
        }
    }
}