using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportsController : Controller
    {
        // GET: /Reports
        public IActionResult Index()
        {
            return View();
        }
    }
}
