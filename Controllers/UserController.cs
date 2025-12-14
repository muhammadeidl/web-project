using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize] // بشكل افتراضي كل الأكشنز محمية
    public class UserController : Controller
    {
        private readonly SporSalonuDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserController(SporSalonuDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ---------------- DASHBOARD ----------------
        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                // سيحوّلك تلقائيًا لصفحة Identity UI Login بسبب Cookie Settings
                return Challenge();
            }

            var userAppointments = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .Where(a => a.UserId == currentUserId)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(userAppointments);
        }

        // ---------------- ACCESS DENIED ----------------
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
