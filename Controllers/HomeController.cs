using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UshiMania.Models;

namespace UshiMania.Controllers
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

        public IActionResult SoDoBan()
        {
            return View();
        }

        public IActionResult OrderPOS()
        {
            return View();
        }

        public IActionResult ManHinhBep()
        {
            return View();
        }

        public IActionResult KhoBaoCao()
        {
            return View();
        }

        public IActionResult GuestOrder([FromQuery] int maBan)
        {
            if (maBan <= 0)
            {
                return BadRequest();
            }

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
