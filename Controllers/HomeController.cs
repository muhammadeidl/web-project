using FitnessCenter.Models;
using Microsoft.AspNetCore.Mvc;



namespace FitnessCenter.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Appointment() 
        { 
            return View();
        }
        public IActionResult AIHair()
        {
            return View();
        }
    }
}
