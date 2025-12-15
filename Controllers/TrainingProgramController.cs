using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;

namespace FitnessCenter.Controllers
{

    public class TrainingProgramController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public TrainingProgramController(SporSalonuDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "admin")]

        public IActionResult Index()
        {
            var trainingprograms = _context.TrainingPrograms
                .Include(s => s.Gym)
                .ToList();

            return View(trainingprograms);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Create(TrainingProgram trainingprogram)
        {
            ModelState.Remove("Gym");

            if (ModelState.IsValid)
            {
                _context.TrainingPrograms.Add(trainingprogram);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Training Program has been successfully added.";
                return RedirectToAction("Index");
            }

            ViewBag.TrainingPrograms = new SelectList(_context.Gyms, "GymId", "Name", trainingprogram.GymId);
            return View(trainingprogram);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var trainingprogram = _context.TrainingPrograms.FirstOrDefault(s => s.TrainingProgramId == id);

            if (trainingprogram == null) return NotFound();

            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", trainingprogram.GymId);
            return View(trainingprogram);
        }

        [HttpPost]
        public IActionResult Edit(TrainingProgram trainingprogram)
        {
            ModelState.Remove("Gym");

            if (ModelState.IsValid)
            {
                var existingTrainingProgram = _context.TrainingPrograms.FirstOrDefault(s => s.TrainingProgramId == trainingprogram.TrainingProgramId);

                if (existingTrainingProgram == null) return NotFound();

                existingTrainingProgram.Name = trainingprogram.Name;
                existingTrainingProgram.Price = trainingprogram.Price;
                existingTrainingProgram.Duration = trainingprogram.Duration;
                existingTrainingProgram.GymId = trainingprogram.GymId;

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Training Program has been successfully updated.";
                return RedirectToAction("Index");
            }

            ViewBag.Gyms = new SelectList(_context.Gyms, "GymId", "Name", trainingprogram.GymId);
            return View(trainingprogram);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var trainingprogram = _context.TrainingPrograms
                .Include(s => s.Gym)
                .FirstOrDefault(s => s.TrainingProgramId == id);

            if (trainingprogram == null) return RedirectToAction("Index");

            return View(trainingprogram);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var trainingprogram = _context.TrainingPrograms.FirstOrDefault(s => s.TrainingProgramId == id);

            if (trainingprogram != null)
            {
                _context.TrainingPrograms.Remove(trainingprogram);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Training Program has been successfully deleted!!";
            }
            else
            {
                TempData["ErrorMessage"] = "Training Program not found!!";
            }

            return RedirectToAction("Index");
        }
    }
}
