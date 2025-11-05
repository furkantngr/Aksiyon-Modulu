using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HierarchicalTaskApp.Models; // <-- Hatanın olduğu 'using'i düzeltiyoruz

namespace HierarchicalTaskApp.Controllers // <-- Namespace'i diğer controller'lar ile aynı yapıyoruz
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
            // Ana sayfa için Login'e yönlendirme daha mantıklı olabilir
            return RedirectToAction("Login", "Auth");
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