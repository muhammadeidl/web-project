using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize] 
    public class UserController : Controller
    {
        private readonly SporSalonuDbContext _context;
        private readonly UserManager<User> _userManager;
        // private readonly SignInManager<User> _signInManager;

        public UserController(SporSalonuDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ---------------- LOGIN ----------------

        //[AllowAnonymous]
        //[HttpGet]
        //public IActionResult Login(string? returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;
        //    return View();
        //}


        //[AllowAnonymous]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        //{
        //    ViewData["ReturnUrl"] = returnUrl;

        //    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        //    {
        //        ModelState.AddModelError("", "Email and password are required.");
        //        return View();
        //    }

        //    var user = await _userManager.FindByEmailAsync(email);
        //    if (user == null)
        //    {
        //        ModelState.AddModelError("", "Invalid email or password.");
        //        return View();
        //    }

        //    var result = await _signInManager.PasswordSignInAsync(
        //        user,
        //        password,
        //        isPersistent: rememberMe,
        //        lockoutOnFailure: false);

        //    if (!result.Succeeded)
        //    {
        //        ModelState.AddModelError("", "Invalid email or password.");
        //        return View();
        //    }

        //    // If there is a returnUrl from [Authorize] redirect, go there first
        //    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //        return Redirect(returnUrl);

        //    // Redirect based on Identity Roles (NOT User.Role)
        //    if (await _userManager.IsInRoleAsync(user, "admin"))
        //        return RedirectToAction("Index", "Admin");

        //    return RedirectToAction("Dashboard");
        //}



        // ---------------- REGISTER ----------------

        //[AllowAnonymous]
        //[HttpGet]
        //public IActionResult Register()
        //{
        //    return View();
        //}

        //[AllowAnonymous]
        //[HttpPost]
        //public async Task<IActionResult> Register(string name, string email, string password)
        //{
        //    if (string.IsNullOrWhiteSpace(name) ||
        //        string.IsNullOrWhiteSpace(email) ||
        //        string.IsNullOrWhiteSpace(password))
        //    {
        //        ModelState.AddModelError("", "Please fill in all fields.");
        //        return View();
        //    }

        //    if (await _userManager.FindByEmailAsync(email) != null)
        //    {
        //        ModelState.AddModelError("", "User already exists with this email.");
        //        return View();
        //    }

        //    var newUser = new User
        //    {
        //        UserName = email,
        //        Email = email,
        //        Name = name
        //    };

        //    var result = await _userManager.CreateAsync(newUser, password);
        //    if (!result.Succeeded)
        //    {
        //        // Show Identity errors for better debugging
        //        foreach (var err in result.Errors)
        //            ModelState.AddModelError("", err.Description);

        //        return View();
        //    }

        //    // Assign Identity role (visitor) - this is the real role system
        //    await _userManager.AddToRoleAsync(newUser, "visitor");

        //    await _signInManager.SignInAsync(newUser, isPersistent: false);

        //    return RedirectToAction("Dashboard");
        //}

        // ---------------- LOGOUT ----------------

        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Logout()
        //{
        //    await _signInManager.SignOutAsync();
        //    return RedirectToAction("Index", "Home");
        //}

        // ---------------- DASHBOARD ----------------
        public async Task<IActionResult> Dashboard()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
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
