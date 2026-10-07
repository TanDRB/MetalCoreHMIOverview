using System.Diagnostics;
using MetalCoreHMIOverview.Models;
using Microsoft.AspNetCore.Mvc;

namespace MetalCoreHMIOverview.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Viscosity()
        {
            return View();
        }

        public IActionResult Machine(int id = 1)
        {
            if (id < 1 || id > 8) return NotFound();
            ViewData["No"] = id;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
