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
        [Authorize(Roles = "admin")]

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
                        DayOfWeek = (FitnessCenter.Models.GymDay)i
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
            ModelState.Remove("CoachTrainingPrograms");
            ModelState.Remove("UnavailableSlots");

            var avs = coach.CoachAvailabilities?.ToList() ?? new List<CoachAvailability>();

            for (int i = 0; i < avs.Count; i++)
            {
                ModelState.Remove($"CoachAvailabilities[{i}].Coach");
                ModelState.Remove($"CoachAvailabilities[{i}].CoachId");
                ModelState.Remove($"CoachAvailabilities[{i}].CoachAvailabilityId");
                ModelState.Remove($"CoachAvailabilities[{i}].StartTime");
                ModelState.Remove($"CoachAvailabilities[{i}].EndTime");

                var av = avs[i];

                if (av.IsClosed || !av.StartTime.HasValue || !av.EndTime.HasValue)
                {
                    av.IsClosed = true;
                    av.StartTime = null;
                    av.EndTime = null;
                }
            }

            // رجّعها للموديل (مهم)
            coach.CoachAvailabilities = avs;

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
                : new HashSet<FitnessCenter.Models.GymDay>();

            for (int i = 0; i < 7; i++)
            {
                var day = (FitnessCenter.Models.GymDay)i;
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
            ModelState.Remove("Gym");
            ModelState.Remove("Appointments");
            ModelState.Remove("CoachTrainingPrograms");
            ModelState.Remove("UnavailableSlots");

            var avs = coach.CoachAvailabilities?.ToList() ?? new List<CoachAvailability>();

            for (int i = 0; i < avs.Count; i++)
            {
                ModelState.Remove($"CoachAvailabilities[{i}].Coach");
                ModelState.Remove($"CoachAvailabilities[{i}].CoachId");
                ModelState.Remove($"CoachAvailabilities[{i}].CoachAvailabilityId");
                ModelState.Remove($"CoachAvailabilities[{i}].StartTime");
                ModelState.Remove($"CoachAvailabilities[{i}].EndTime");

                var av = avs[i];

                if (av.IsClosed || !av.StartTime.HasValue || !av.EndTime.HasValue)
                {
                    av.IsClosed = true;
                    av.StartTime = null;
                    av.EndTime = null;
                }
            }

            coach.CoachAvailabilities = avs;

            if (!ModelState.IsValid)
            {
                ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", coach.GymId);
                return View(coach);
            }

            var existingCoach = _context.Coaches
                .Include(e => e.CoachAvailabilities)
                .FirstOrDefault(e => e.CoachId == coach.CoachId);

            if (existingCoach == null)
                return RedirectToAction(nameof(Index));

            existingCoach.Name = coach.Name;
            existingCoach.Expertise = coach.Expertise;
            existingCoach.GymId = coach.GymId;

            _context.CoachAvailability.RemoveRange(existingCoach.CoachAvailabilities);

            foreach (var av in avs.OrderBy(a => a.DayOfWeek))
            {
                existingCoach.CoachAvailabilities.Add(new CoachAvailability
                {
                    DayOfWeek = av.DayOfWeek,
                    IsClosed = av.IsClosed,
                    StartTime = av.StartTime,
                    EndTime = av.EndTime
                });
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
