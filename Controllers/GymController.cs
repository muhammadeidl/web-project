//using FitnessCenter.Data;
//using FitnessCenter.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace FitnessCenter.Controllers
//{
//    [Authorize(Roles = "admin")]
//    public class GymController : Controller
//    {
//        private readonly SporSalonuDbContext _context;

//        public GymController(SporSalonuDbContext context)
//        {
//            _context = context;
//        }

//        public IActionResult Index()
//        {
//            var gyms = _context.Gyms
//                .Include(s => s.WorkingHours)
//                .ToList();

//            return View(gyms);
//        }

//        // ============================
//        // CREATE (GET)
//        // ============================
//        [HttpGet]
//        public IActionResult Create()
//        {
//            var model = new Gym();

//            for (int i = 0; i < 7; i++)
//            {
//                model.WorkingHours.Add(new GymWorkingHours
//                {
//                    DayOfWeek = (FitnessCenter.Models.DayOfWeek)i,
//                    IsClosed = false,
//                    StartTime = null,
//                    EndTime = null
//                });
//            }

//            return View(model);
//        }


//        // =============================
//        // CREATE (POST)
//        // =============================
//        [HttpPost]
//        public IActionResult Create(Gym gym)
//        {
//            if (gym.WorkingHours != null)
//            {
//                for (int i = 0; i < gym.WorkingHours.Count; i++)
//                {
//                    var wh = gym.WorkingHours[i];

//                    // إزالة أخطاء التحقق لـ Gym و GymId
//                    ModelState.Remove($"WorkingHours[{i}].Gym");
//                    ModelState.Remove($"WorkingHours[{i}].GymId");

//                    if (wh.IsClosed)
//                    {
//                        wh.StartTime = null;
//                        wh.EndTime = null;

//                        // تجاوز أخطاء التحقق يدوياً عندما يكون اليوم مغلقًا
//                        ModelState.Remove($"WorkingHours[{i}].StartTime");
//                        ModelState.Remove($"WorkingHours[{i}].EndTime");
//                    }
//                }
//            }

//            // إزالة أخطاء التحقق غير المرغوبة على الكيانات الرئيسية
//            ModelState.Remove("GymId");

//            if (ModelState.IsValid)
//            {
//                _context.Gyms.Add(gym);
//                _context.SaveChanges();
//                return RedirectToAction("Index");
//            }

//            return View(gym);
//        }


//        // =============================
//        // EDIT (GET)
//        // =============================
//        [HttpGet]
//        public IActionResult Edit(int id)
//        {
//            var gym = _context.Gyms
//                .Include(g => g.WorkingHours)
//                .FirstOrDefault(g => g.GymId == id);

//            if (gym == null)
//                return NotFound();

//            return View(gym);
//        }

//        // =============================
//        // EDIT (POST)
//        // =============================
//        // في FitnessCenter.Controllers/GymController.cs

//        [HttpPost]
//        public IActionResult Edit(Gym gym)
//        {
//            // إزالة أخطاء التحقق غير المرغوبة على الكيانات الرئيسية
//            ModelState.Remove("GymId");

//            if (gym.WorkingHours != null)
//            {
//                // نستخدم حلقة for لضمان الحصول على الفهرس i
//                for (int i = 0; i < gym.WorkingHours.Count; i++)
//                {
//                    var wh = gym.WorkingHours[i];

//                    // إزالة أخطاء التحقق لـ Gym و GymId و Id (الآن قمنا بتنظيفها جميعاً)
//                    ModelState.Remove($"WorkingHours[{i}].Gym");
//                    ModelState.Remove($"WorkingHours[{i}].GymId");
//                    ModelState.Remove($"WorkingHours[{i}].Id");

//                    // إذا كان اليوم مغلقًا
//                    if (wh.IsClosed)
//                    {
//                        wh.StartTime = null;
//                        wh.EndTime = null;

//                        // تجاوز أخطاء التحقق يدوياً للأوقات المغلقة
//                        ModelState.Remove($"WorkingHours[{i}].StartTime");
//                        ModelState.Remove($"WorkingHours[{i}].EndTime");
//                    }
//                    // إذا كان اليوم مفتوحاً، ولكن الـ Model Binder فشل في ربط قيم الوقت الفارغة (وهذا هو سبب المشكلة)
//                    else
//                    {
//                        // 🛑 الإصلاح الأخير: تجاوز أخطاء التحقق لـ StartTime و EndTime إذا لم يتم تعبئة الحقل
//                        // هذا يمنع فشل التحقق في المرة الأولى بسبب القيم الفارغة المرسلة من حقول الوقت
//                        if (!wh.StartTime.HasValue && ModelState.ContainsKey($"WorkingHours[{i}].StartTime"))
//                        {
//                            ModelState.Remove($"WorkingHours[{i}].StartTime");
//                        }
//                        if (!wh.EndTime.HasValue && ModelState.ContainsKey($"WorkingHours[{i}].EndTime"))
//                        {
//                            ModelState.Remove($"WorkingHours[{i}].EndTime");
//                        }
//                    }
//                }
//            }

//            // 🛑 التحقق من صحة النموذج بعد تنظيفه
//            if (!ModelState.IsValid)
//            {
//                // إذا فشل التحقق، عد إلى View لعرض الأخطاء (للمرة الأولى فقط)
//                return View(gym);
//            }

//            // ---------------------------------
//            // منطق الحفظ والتحديث (PRG)
//            // ---------------------------------

//            var existingGym = _context.Gyms
//                .Include(g => g.WorkingHours)
//                .FirstOrDefault(g => g.GymId == gym.GymId);

//            if (existingGym == null)
//                return NotFound();

//            existingGym.Name = gym.Name;
//            existingGym.Location = gym.Location;

//            // حذف الأوقات القديمة وإضافة الأوقات الجديدة
//            _context.GymWorkingHours.RemoveRange(existingGym.WorkingHours);

//            foreach (var wh in gym.WorkingHours)
//            {
//                existingGym.WorkingHours.Add(new GymWorkingHours
//                {
//                    DayOfWeek = wh.DayOfWeek,
//                    IsClosed = wh.IsClosed,
//                    StartTime = wh.StartTime,
//                    EndTime = wh.EndTime
//                });
//            }

//            _context.SaveChanges();
//            // التوجيه إلى Index بعد الحفظ بنجاح (سلوك PRG)
//            return RedirectToAction("Index");
//        }

//        // =============================
//        // DELETE (GET)
//        // =============================
//        [HttpGet]
//        public IActionResult Delete(int id)
//        {
//            var gym = _context.Gyms
//                .Include(g => g.WorkingHours)
//                .FirstOrDefault(g => g.GymId == id);

//            if (gym == null)
//                return NotFound();

//            return View(gym);
//        }

//        // =============================
//        // DELETE (POST)
//        // =============================
//        [HttpPost, ActionName("Delete")]
//        public IActionResult DeleteConfirmed(int id)
//        {
//            var gym = _context.Gyms
//                .Include(g => g.WorkingHours)
//                .FirstOrDefault(g => g.GymId == id);

//            if (gym != null)
//            {
//                _context.GymWorkingHours.RemoveRange(gym.WorkingHours);
//                _context.Gyms.Remove(gym);
//                _context.SaveChanges();
//            }

//            return RedirectToAction("Index");
//        }
//    }
//}using FitnessCenter.Data;
using FitnessCenter.Data;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "admin")]
    public class GymController : Controller
    {
        private readonly SporSalonuDbContext _context;

        public GymController(SporSalonuDbContext context)
        {
            _context = context;
        }

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
                    DayOfWeek = (FitnessCenter.Models.DayOfWeek)i,
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
                    // 🛑 تنظيف أخطاء الربط حتى لو لم يتم تحديد IsClosed
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
            // 1. تنظيف ModelState لضمان نجاح التحقق في المحاولة الأولى
            ModelState.Remove("GymId");

            if (gym.WorkingHours != null)
            {
                for (int i = 0; i < gym.WorkingHours.Count; i++)
                
                {
                    var wh = gym.WorkingHours[i];

                    ModelState.Remove($"WorkingHours[{i}].Gym");
                    ModelState.Remove($"WorkingHours[{i}].GymId");
                    ModelState.Remove($"WorkingHours[{i}].Id"); // إزالة Id لتجنب أخطاء تتبع EF

                    if (wh.IsClosed)
                    {
                        wh.StartTime = null;
                        wh.EndTime = null;
                    }

                    // 🛑 الحل الأكثر قوة: مسح أخطاء ربط الوقت في كل الأحوال.
                    // هذا ضروري لأن ربط النموذج يحاول تحويل السلسلة الفارغة ("") إلى TimeSpan? ويفشل.
                    // نحن نقوم بإزالة الأخطاء يدوياً للـ StartTime و EndTime لنتجاوز فشل الربط.
                    ModelState.Remove($"WorkingHours[{i}].StartTime");
                    ModelState.Remove($"WorkingHours[{i}].EndTime");
                }
            }

            // 🛑 2. التحقق من صحة النموذج بعد تنظيفه
            if (!ModelState.IsValid)
            {
                // إذا فشل التحقق، عد إلى View لعرض الأخطاء (الاسم والموقع فقط)
                return RedirectToAction("Index");                //****************************************************************************************************
            }

            // ---------------------------------
            // منطق الحفظ والتحديث
            // ---------------------------------

            var existingGym = _context.Gyms
                .Include(g => g.WorkingHours)
                .FirstOrDefault(g => g.GymId == gym.GymId);

            if (existingGym == null)
                return NotFound();

            existingGym.Name = gym.Name;
            existingGym.Location = gym.Location;

            // حذف الأوقات القديمة وإضافة الأوقات الجديدة
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
            // التوجيه إلى Index بعد الحفظ بنجاح (سلوك PRG)
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