using HierarchicalTaskApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq; // .Select() için eklendi
using System.Threading.Tasks; // async Task için eklendi

namespace HierarchicalTaskApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ITaskRepository _repository;

        public AuthController(ITaskRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            HttpContext.Session.Clear();
            
            var users = await _repository.GetUsersAsync();
            
            // HATA BURADAYDI: Bu dönüştürme işlemi sizde eksik kalmış
            var userList = users.Select(u => new SelectListItem {
                Value = u.Id.ToString(),
                Text = $"{u.Id} - {u.FullName}" // .FullName kullan
            }).ToList();
            
            ViewBag.Users = new SelectList(userList, "Value", "Text");
            return View();
        }

        [HttpPost]
        public IActionResult Login(int selectedUserId)
        {
            if (selectedUserId == 0)
            {
                return RedirectToAction("Login");
            }
            HttpContext.Session.SetInt32("UserId", selectedUserId);
            return RedirectToAction("Index", "Task");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}