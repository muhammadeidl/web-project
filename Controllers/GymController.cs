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
        // OLUŞTUR (GET & POST)
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
                    // 🛑 IsClosed seçilmemiş olsa bile model bağlama hatalarını temizle
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
        // DÜZENLE (GET)
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
        // DÜZENLE (POST)
        // =============================
        [HttpPost]
        public IActionResult Edit(Gym gym)
        {
            // 1. İlk denemede doğrulamanın başarılı olması için ModelState'i temizle
            ModelState.Remove("GymId");

            if (gym.WorkingHours != null)
            {
                for (int i = 0; i < gym.WorkingHours.Count; i++)
                {
                    var wh = gym.WorkingHours[i];

                    ModelState.Remove($"WorkingHours[{i}].Gym");
                    ModelState.Remove($"WorkingHours[{i}].GymId");
                    ModelState.Remove($"WorkingHours[{i}].Id"); // EF izleme hatalarını önlemek için Id'yi kaldır

                    if (wh.IsClosed)
                    {
                        wh.StartTime = null;
                        wh.EndTime = null;
                    }

                    // 🛑 En güçlü çözüm: Her durumda zaman bağlama hatalarını temizle.
                    // Bu gereklidir çünkü model bağlayıcı boş dizeyi ("") TimeSpan? türüne dönüştürmeye çalışır ve başarısız olur.
                    // Bağlama başarısızlığını aşmak için StartTime ve EndTime hatalarını manuel olarak kaldırıyoruz.
                    ModelState.Remove($"WorkingHours[{i}].StartTime");
                    ModelState.Remove($"WorkingHours[{i}].EndTime");
                }
            }

            // 🛑 2. Temizlendikten sonra modelin geçerliliğini kontrol et
            if (!ModelState.IsValid)
            {
                // Doğrulama başarısız olursa, hataları göstermek için View'a geri dön (Sadece İsim ve Konum)
                return View(gym);
            }


            // ---------------------------------
            // Kaydetme ve Güncelleme Mantığı
            // ---------------------------------

            var existingGym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == gym.GymId);

            if (existingGym == null)
                return NotFound();

            existingGym.Name = gym.Name;
            existingGym.Location = gym.Location;

            // Eski çalışma saatlerini sil ve yenilerini ekle
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
            // Başarıyla kaydedildikten sonra Index'e yönlendir (PRG deseni)
            return RedirectToAction("Index");
        }

        // =============================
        // SİL (GET)
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
        // SİL (POST)
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
