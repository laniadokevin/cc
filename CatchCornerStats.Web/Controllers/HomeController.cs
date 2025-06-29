using CatchCornerStats.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CatchCornerStats.Web.Controllers
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

        public IActionResult Privacy()
        {
            return View();
        }

        // Stats API Views - Core Analysis Views
        public IActionResult GetAverageLeadTime()
        {
            return View();
        }

        public IActionResult GetLeadTimeBreakdown()
        {
            return View();
        }

        public IActionResult GetBookingsByDay()
        {
            return View();
        }

        public IActionResult GetBookingsByStartTime()
        {
            return View();
        }

        public IActionResult GetBookingDurationBreakdown()
        {
            return View();
        }

        public IActionResult GetMonthlyReport()
        {
            return View();
        }

        public IActionResult GetSportComparison()
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
