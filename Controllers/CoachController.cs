using System;
using System.Collections.Generic;
using System.Linq;
using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "admin")]
    public class CoachController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public CoachController(SporSalonuDbContext context)
        {
            _context = context;
        }

        // ===========================
        //  LIST ALL COACHES
        // ===========================
        public IActionResult Index()
        {
            var coaches = _context.Coaches
                .Include(e => e.Gym)
                .Include(e => e.CoachAvailabilities)
                .ToList();

            return View(coaches);
        }

        // ===========================
        //  CREATE (GET)
        // ===========================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name");

            // Initialize 7 days
            var model = new Coach
            {
                CoachAvailabilities = Enumerable.Range(0, 7)
                    .Select(i => new CoachAvailability
                    {
                        DayOfWeek = (FitnessCenter.Models.DayOfWeek)i
                    })
                    .ToList()
            };

            return View(model);
        }

        // ===========================
        //  CREATE (POST)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Coach coach)
        {
            ModelState.Remove("Gym");
            ModelState.Remove("Appointments");

            if (coach.CoachAvailabilities != null)
            {
                // Remove navigation-related model state errors
                for (int i = 0; i < coach.CoachAvailabilities.Count; i++)
                {
                    ModelState.Remove($"CoachAvailabilities[{i}].Coach");
                }

                // Keep only rows where both times are set
                coach.CoachAvailabilities = coach.CoachAvailabilities
                    .Where(a => a.StartTime != TimeSpan.Zero && a.EndTime != TimeSpan.Zero)
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", coach.GymId);
                return View(coach);
            }

            _context.Coaches.Add(coach);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ===========================
        //  EDIT (GET)
        // ===========================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var coach = _context.Coaches
                .Include(e => e.CoachAvailabilities)
                .FirstOrDefault(e => e.CoachId == id);

            if (coach == null)
                return NotFound();

            // Ensure 7 days exist (fill missing days)
            var existingDays = coach.CoachAvailabilities != null
                ? coach.CoachAvailabilities.Select(a => a.DayOfWeek).ToHashSet()
                : new HashSet<FitnessCenter.Models.DayOfWeek>();

            for (int i = 0; i < 7; i++)
            {
                var day = (FitnessCenter.Models.DayOfWeek)i;
                if (!existingDays.Contains(day))
                {
                    coach.CoachAvailabilities.Add(new CoachAvailability
                    {
                        DayOfWeek = day
                    });
                }
            }

            // Order by day for consistent indexing with the view
            coach.CoachAvailabilities = coach.CoachAvailabilities
                .OrderBy(a => a.DayOfWeek)
                .ToList();

            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", coach.GymId);

            return View(coach);
        }

        // ===========================
        //  EDIT (POST)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Coach coach)
        {
            if (ModelState.IsValid)
            {
                ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", coach.GymId);
                return View(coach);
            }

            ModelState.Remove("Gym");
            ModelState.Remove("Appointments");

            if (coach.CoachAvailabilities != null)
            {
                for (int i = 0; i < coach.CoachAvailabilities.Count; i++)
                {
                    ModelState.Remove($"CoachAvailabilities[{i}].Coach");
                }

                // Only keep rows where user entered both times
                coach.CoachAvailabilities = coach.CoachAvailabilities
                    .Where(a => a.StartTime != TimeSpan.Zero && a.EndTime != TimeSpan.Zero)
                    .ToList();
            }



            var existingCoach = _context.Coaches
                .Include(e => e.CoachAvailabilities)
                .FirstOrDefault(e => e.CoachId == coach.CoachId);

            if (existingCoach == null)
                return RedirectToAction("Index");

            // Update scalar properties
            existingCoach.Name = coach.Name;
            existingCoach.Expertise = coach.Expertise;
            existingCoach.GymId = coach.GymId;

            // Remove old availabilities
            if (existingCoach.CoachAvailabilities != null && existingCoach.CoachAvailabilities.Any())
            {
                _context.CoachAvailability.RemoveRange(existingCoach.CoachAvailabilities);
            }

            existingCoach.CoachAvailabilities = new List<CoachAvailability>();

            // Add new availabilities
            if (coach.CoachAvailabilities != null)
            {
                foreach (var availability in coach.CoachAvailabilities)
                {
                    existingCoach.CoachAvailabilities.Add(new CoachAvailability
                    {
                        DayOfWeek = availability.DayOfWeek,
                        StartTime = availability.StartTime,
                        EndTime = availability.EndTime
                    });
                }
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        // ===========================
        //  DELETE (GET)
        // ===========================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var coach = _context.Coaches
                .Include(e => e.Gym)
                .Include(e => e.CoachAvailabilities)
                .FirstOrDefault(e => e.CoachId == id);

            if (coach == null)
                return RedirectToAction("Index");

            return View(coach);
        }

        // ===========================
        //  DELETE (POST)
        // ===========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var coach = _context.Coaches
                .Include(e => e.CoachAvailabilities)
                .FirstOrDefault(e => e.CoachId == id);

            if (coach != null)
            {
                if (coach.CoachAvailabilities != null && coach.CoachAvailabilities.Any())
                {
                    _context.CoachAvailability.RemoveRange(coach.CoachAvailabilities);
                }

                _context.Coaches.Remove(coach);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
