
using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace FitnessCenter.Controllers
{
    public class GymController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public GymController(SporSalonuDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "admin")]

        public IActionResult Index()
        {
            var gyms = _context.Gyms
                .Include(s => s.WorkingHours)
                .ToList();

            return View(gyms);
        }

        // ============================
        // CREATE (GET & POST)
        // ============================
        [HttpGet]
        public IActionResult Create()
        {
            var model = new Gym();

            for (int i = 0; i < 7; i++)
            {
                model.WorkingHours.Add(new GymWorkingHours
                {
                    DayOfWeek = (FitnessCenter.Models.GymDay)i,
                    IsClosed = false,
                    StartTime = null,
                    EndTime = null
                });
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(Gym gym)
        {
            if (gym.WorkingHours != null)
            {
                for (int i = 0; i < gym.WorkingHours.Count; i++)
                {
                    var wh = gym.WorkingHours[i];
                    ModelState.Remove($"WorkingHours[{i}].Gym");
                    ModelState.Remove($"WorkingHours[{i}].GymId");

                    if (wh.IsClosed)
                    {
                        wh.StartTime = null;
                        wh.EndTime = null;
                        ModelState.Remove($"WorkingHours[{i}].StartTime");
                        ModelState.Remove($"WorkingHours[{i}].EndTime");
                    }
                    // 
                    else
                    {
                        ModelState.Remove($"WorkingHours[{i}].StartTime");
                        ModelState.Remove($"WorkingHours[{i}].EndTime");
                    }
                }
            }
            ModelState.Remove("GymId");

            if (ModelState.IsValid)
            {
                _context.Gyms.Add(gym);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(gym);
        }


        // =============================
        // EDIT (GET)
        // =============================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var gym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == id);
            if (gym == null) return NotFound();
            return View(gym);
        }

        // =============================
        // EDIT (POST)
        // =============================
        [HttpPost]
        public IActionResult Edit(Gym gym)
        {
            ModelState.Remove("GymId");

            if (gym.WorkingHours != null)
            {
                for (int i = 0; i < gym.WorkingHours.Count; i++)
                
                {
                    var wh = gym.WorkingHours[i];

                    ModelState.Remove($"WorkingHours[{i}].Gym");
                    ModelState.Remove($"WorkingHours[{i}].GymId");
                    ModelState.Remove($"WorkingHours[{i}].Id");  

                    if (wh.IsClosed)
                    {
                        wh.StartTime = null;
                        wh.EndTime = null;
                    }

                    ModelState.Remove($"WorkingHours[{i}].StartTime");
                    ModelState.Remove($"WorkingHours[{i}].EndTime");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(gym);
            }



            var existingGym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == gym.GymId);

            if (existingGym == null)
                return NotFound();

            existingGym.Name = gym.Name;
            existingGym.Location = gym.Location;

            _context.GymWorkingHours.RemoveRange(existingGym.WorkingHours);

            foreach (var wh in gym.WorkingHours)
            {
                existingGym.WorkingHours.Add(new GymWorkingHours
                {
                    DayOfWeek = wh.DayOfWeek,
                    IsClosed = wh.IsClosed,
                    StartTime = wh.StartTime,
                    EndTime = wh.EndTime
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        // =============================
        // DELETE (GET)
        // =============================
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var gym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == id);
            if (gym == null) return RedirectToAction("Index");
            return View(gym);
        }

        // =============================
        // DELETE (POST)
        // =============================
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var gym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == id);

            if (gym != null)
            {
                _context.GymWorkingHours.RemoveRange(gym.WorkingHours);
                _context.Gyms.Remove(gym);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
