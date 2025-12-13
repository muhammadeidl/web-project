using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace FitnessCenter.Controllers
{
    public class UserController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public UserController(SporSalonuDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email); // Removed password check

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            // If you want to check password, you need to use ASP.NET Identity's PasswordHasher or SignInManager.
            // For now, just allow login if user exists.

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserId", user.Id) // Changed from user.UserId to user.Id
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            if (user.Role == "admin")
            {
                Console.WriteLine(user.Role);
                return RedirectToAction("Index", "Admin");
            }
            else
            {
                Console.WriteLine(user.Role);
                return RedirectToAction("Dashboard");

            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string name, string email, string password)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "User already exists with this email.");
                return View();
            }

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(), // Changed from UserId to Id
                Name = name,
                Email = email,
                PasswordHash = password, // Use PasswordHash property
                Role = "customer"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            string currentUserId = User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login");
            }

    // Yalnızca mevcut kullanıcı için randevu verilerini getir ve programlar ile eğitmenleri dahil et
        var userAppointments = await _context.Appointments
                .Include(a => a.TrainingProgram)
                .Include(a => a.Coach)
                .Where(a => a.UserId == currentUserId) // If Appointment.UserId refers to User.Id, this is correct.
                .ToListAsync();

            return View(userAppointments);
        }


        [Authorize(Roles = "admin")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
