using HierarchicalTaskApp.Models;
using HierarchicalTaskApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Fluent;
using System.Threading.Tasks; // async Task
using System.Linq; // .GroupBy(), .Select(), .ToList()
using System; // DateTime
using System.Collections.Generic; // List<T>

namespace HierarchicalTaskApp.Controllers
{
    public class TaskController : Controller
    {
        private readonly ITaskRepository _repository;

        public TaskController(ITaskRepository repository)
        {
            _repository = repository;
        }

        // --- YARDIMCI METOTLAR ---

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private IActionResult? CheckSession()
        {
            if (GetCurrentUserId() == 0)
            {
                return RedirectToAction("Login", "Auth");
            }
            return null;
        }

        // --- ANA SAYFA (INDEX) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var userId = GetCurrentUserId();
            var user = await _repository.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                return RedirectToAction("Login", "Auth"); 
            }

            var vm = new TaskDashboardViewModel
            {
                LoggedInUserRole = user.Role
            };
 
            // Rol'e göre ilgili listeleri çek
            switch (user.Role)
            {
                case UserRole.Manager:
                    vm.HierarchicalTasks = await _repository.GetTasksForUserAsync(userId);
                    vm.TasksAssignedByMe = await _repository.GetTasksAssignedByUserAsync(userId);
                    break;
                case UserRole.TeamLeader:
                    vm.HierarchicalTasks = await _repository.GetTasksForUserAsync(userId);
                    vm.TasksAssignedToMe = await _repository.GetTasksAssignedToUserAsync(userId);
                    break;
                case UserRole.Employee:
                    vm.TasksAssignedToMe = await _repository.GetTasksAssignedToUserAsync(userId);
                    break;
            }

            // Grafik verisini hesapla (Lider veya Yönetici ise)
            if (user.Role != UserRole.Employee && vm.HierarchicalTasks.Any())
            {
                var statusCounts = vm.HierarchicalTasks
                    .GroupBy(task => task.Status)
                    .ToDictionary(group => group.Key, group => group.Count());
                
                statusCounts.TryAdd(Models.TaskStatus.Todo, 0);
                statusCounts.TryAdd(Models.TaskStatus.InProgress, 0);
                statusCounts.TryAdd(Models.TaskStatus.Done, 0);
                
                vm.ChartLabels = new List<string> { "Beklemede", "Devam Ediyor", "Tamamlandı" };
                vm.ChartData = new List<int>
                {
                    statusCounts[Models.TaskStatus.Todo],
                    statusCounts[Models.TaskStatus.InProgress],
                    statusCounts[Models.TaskStatus.Done]
                };
            }

            ViewBag.Users = await _repository.GetUsersAsync();
            return View(vm);
        }

        // --- GÖREV DETAYLARI ---
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var task = await _repository.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var loggedInUserId = GetCurrentUserId();
            var loggedInUser = await _repository.GetUserByIdAsync(loggedInUserId);
            var assignee = await _repository.GetUserByIdAsync(task.AssigneeId);
            
            ViewBag.AssigneesManagerId = assignee?.ManagerId ?? 0;
            ViewBag.UserRole = loggedInUser?.Role ?? UserRole.Employee;
            ViewBag.CurrentUserId = loggedInUserId;

            // --- YENİ EKLENDİ (AKTARMA YETKİ KONTROLÜ) ---
            bool isAssignee = (loggedInUser != null && loggedInUser.Role != UserRole.Employee && task.AssigneeId == loggedInUserId);
            bool isDirectManager = (loggedInUserId == (assignee?.ManagerId ?? 0));
            bool isHierarchicalManager = false;
            if (loggedInUser != null && loggedInUser.Role == UserRole.Manager)
            {
                isHierarchicalManager = await _repository.IsUserSubordinateOfAsync(task.AssigneeId, loggedInUserId);
            }
            
            // Bu üç durumdan biri doğruysa, butonu göster
            ViewBag.CanTransfer = (isAssignee || isDirectManager || isHierarchicalManager);
            // --- EKLEME BİTTİ ---

            ViewBag.History = await _repository.GetHistoryForTaskAsync(id);
            ViewBag.Users = await _repository.GetUsersAsync();
            ViewBag.Comments = await _repository.GetCommentsForTaskAsync(id);
            ViewBag.WorkLogs = await _repository.GetWorkLogsForTaskAsync(id);
            ViewBag.Categories = await _repository.GetAllCategoriesAsync();
            
            return View(task);
        }

        // --- YORUM EKLEME ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTaskComment(int actionTaskId, string commentText) 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["CommentError"] = "Yorum alanı boş bırakılamaz.";
            }
            else
            {
                var comment = new TaskComment
                {
                    ActionTaskId = actionTaskId,
                    CommentText = commentText,
                    AuthorUserId = GetCurrentUserId()
                };
                await _repository.AddTaskCommentAsync(comment);
            }

            return RedirectToAction("Details", new { id = actionTaskId });
        }

        // --- ZAMAN KAYDI EKLEME (WORK LOG) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkLog(int actionTaskId, DateTime logDate, decimal hoursSpent, string workSummary) 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var workLog = new WorkLog
            {
                ActionTaskId = actionTaskId,
                LogDate = logDate,
                HoursSpent = hoursSpent,
                WorkSummary = workSummary,
                UserId = GetCurrentUserId()
            };

            if (!TryValidateModel(workLog))
            {
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                TempData["WorkLogError"] = string.Join("; ", errorMessages);
            }
            else
            {
                await _repository.AddWorkLogAsync(workLog);
                TempData["WorkLogSuccess"] = "Zaman kaydı başarıyla eklendi.";
            }

            return RedirectToAction("Details", new { id = actionTaskId });
        }

        // --- YENİ GÖREV OLUŞTURMA ---
        [HttpGet]
        public async Task<IActionResult> Create() 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            // "ID - Ad Soyad" formatında tüm kullanıcılar (departman=null)
            var allUsers = await _repository.GetUsersByDepartmentAsync(null);
            var userList = allUsers.Select(u => new SelectListItem {
                Value = u.Id.ToString(),
                Text = $"{u.Id} - {u.FullName}"
            }).ToList();
            ViewBag.Users = new SelectList(userList, "Value", "Text");

            ViewBag.Departments = new SelectList(await _repository.GetDepartmentsAsync(), "Id", "Name");

            return View(new ActionTask { Deadline = DateTime.Now.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActionTask task) 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            if (ModelState.IsValid)
            {
                task.AssignerId = GetCurrentUserId();
                task.Status = Models.TaskStatus.Todo; 

                await _repository.CreateTaskAsync(task);
                return RedirectToAction("Index");
            }
            
            // Hata Durumu (Form Geçersizse): Dropdown'ları yeniden doldur
            var users = await _repository.GetUsersByDepartmentAsync(task.DepartmentId);
            var userList = users.Select(u => new SelectListItem {
                Value = u.Id.ToString(),
                Text = $"{u.Id} - {u.FullName}"
            }).ToList();
            ViewBag.Users = new SelectList(userList, "Value", "Text", task.AssigneeId);

            ViewBag.Departments = new SelectList(await _repository.GetDepartmentsAsync(), "Id", "Name", task.DepartmentId);
            
            return View(task);
        }

        // --- DURUM GÜNCELLEME ---
        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id) 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var task = await _repository.GetTaskByIdAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            
            var vm = new UpdateStatusViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                CurrentStatus = task.Status,
                NewStatus = task.Status
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel vm) 
        { 
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            if (ModelState.IsValid)
            {
                var loggedInUserId = GetCurrentUserId();
                await _repository.UpdateTaskStatusAsync(vm.TaskId, loggedInUserId, (HierarchicalTaskApp.Models.TaskStatus)vm.NewStatus, vm.Comment);
                return RedirectToAction("Details", new { id = vm.TaskId });
            }
            
            return View(vm);
        }

        // --- ÖNCELİK GÜNCELLEME ---
        [HttpGet]
        public async Task<IActionResult> UpdatePriority(int id) 
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var loggedInUserId = GetCurrentUserId();
            var task = await _repository.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            var assignee = await _repository.GetUserByIdAsync(task.AssigneeId);
            
            // Güvenlik: Sadece atanan kişinin yöneticisi değiştirebilir
            if (loggedInUserId != assignee?.ManagerId)
            {
                TempData["WorkLogError"] = "Sadece göreve atanan kişinin yöneticisi önceliği değiştirebilir.";
                return RedirectToAction("Details", new { id = id });
            }
            
            var vm = new UpdatePriorityViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                CurrentPriority = task.Priority,
                NewPriority = task.Priority
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(UpdatePriorityViewModel vm) 
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var loggedInUserId = GetCurrentUserId();
            var task = await _repository.GetTaskByIdAsync(vm.TaskId);
            if (task == null) return NotFound();

            var assignee = await _repository.GetUserByIdAsync(task.AssigneeId);
            
            // Güvenlik (POST için tekrar)
            if (loggedInUserId != assignee?.ManagerId)
            {
                TempData["WorkLogError"] = "Yetkisiz işlem denemesi.";
                return RedirectToAction("Details", new { id = vm.TaskId });
            }

            if (ModelState.IsValid)
            {
                if (task.Priority != vm.NewPriority)
                {
                    await _repository.UpdateTaskPriorityAsync(
                        vm.TaskId,
                        loggedInUserId,
                        vm.NewPriority,
                        task.Priority
                    );
                    TempData["WorkLogSuccess"] = "Görev önceliği başarıyla güncellendi.";
                }
                return RedirectToAction("Details", new { id = vm.TaskId });
            }

            return View(vm);
        }

        // --- GÖREV AKTARMA (DELEGASYON) (SON GÜNCELLEME) ---
        [HttpGet]
        public async Task<IActionResult> TransferTask(int id) // id = TaskId
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var loggedInUserId = GetCurrentUserId();
            var loggedInUser = await _repository.GetUserByIdAsync(loggedInUserId);
            var task = await _repository.GetTaskByIdAsync(id);

            if (task == null || loggedInUser == null)
            {
                return NotFound();
            }

            // ... (Güvenlik Kuralı: isAssignee, isDirectManager, isHierarchicalManager... kod aynı kalır) ...
            var assignee = await _repository.GetUserByIdAsync(task.AssigneeId);
            var assigneesManagerId = assignee?.ManagerId ?? 0;
            bool isAssignee = (loggedInUser.Role != UserRole.Employee && task.AssigneeId == loggedInUserId);
            bool isDirectManager = (loggedInUserId == assigneesManagerId);
            bool isHierarchicalManager = false;
            if (loggedInUser.Role == UserRole.Manager)
            {
                isHierarchicalManager = await _repository.IsUserSubordinateOfAsync(task.AssigneeId, loggedInUserId);
            }
            if (!isAssignee && !isDirectManager && !isHierarchicalManager)
            {
                TempData["WorkLogError"] = "Bu görevi aktarma yetkiniz yok.";
                return RedirectToAction("Details", new { id = id });
            }

            // --- DEĞİŞİKLİK BURADA BAŞLIYOR ---

            // 1. ViewModel'i ve IsFlagged durumunu doldur
            var vm = new TransferTaskViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                IsFlagged = (task.Flag == FlagStatus.FlaggedAsIncorrect) // <-- ÖNEMLİ
            };

            List<User> assignableUsers;

            // 2. Kullanıcı listesini doldur
            // Eğer görev hatalıysa VEYA aktaran kişi Yönetici ise, TÜM kullanıcıları listele (JavaScript filtrelenecek)
            if (task.Flag == FlagStatus.FlaggedAsIncorrect || loggedInUser.Role == UserRole.Manager)
            {
                assignableUsers = await _repository.GetUsersByDepartmentAsync(null); 
            }
            else // Liderse VE görev hatalı değilse, SADECE kendi departmanını listele
            {
                var departmentUsers = await _repository.GetUsersByDepartmentAsync(loggedInUser.DepartmentId);
                assignableUsers = departmentUsers.Where(u => u.Id != task.AssigneeId).ToList();
            }
            
            var userList = assignableUsers.Select(u => new SelectListItem {
                Value = u.Id.ToString(),
                Text = $"{u.Id} - {u.FullName}"
            }).ToList();
            ViewBag.Users = new SelectList(userList, "Value", "Text");
            
            // 3. (YENİ) Departman dropdown'ını doldur
            ViewBag.Departments = new SelectList(await _repository.GetDepartmentsAsync(), "Id", "Name", task.DepartmentId);

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferTask(TransferTaskViewModel vm)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var loggedInUserId = GetCurrentUserId();
            var loggedInUser = await _repository.GetUserByIdAsync(loggedInUserId);
            var task = await _repository.GetTaskByIdAsync(vm.TaskId);

            if (task == null || loggedInUser == null) return NotFound();

            // ... (Güvenlik Kuralı: isAssignee, isDirectManager, isHierarchicalManager... kod aynı kalır) ...
            var assignee = await _repository.GetUserByIdAsync(task.AssigneeId);
            var assigneesManagerId = assignee?.ManagerId ?? 0;
            bool isAssignee = (loggedInUser.Role != UserRole.Employee && task.AssigneeId == loggedInUserId);
            bool isDirectManager = (loggedInUserId == assigneesManagerId);
            bool isHierarchicalManager = false;
            if (loggedInUser.Role == UserRole.Manager)
            {
                isHierarchicalManager = await _repository.IsUserSubordinateOfAsync(task.AssigneeId, loggedInUserId);
            }
            if (!isAssignee && !isDirectManager && !isHierarchicalManager)
            {
                TempData["WorkLogError"] = "Bu görevi aktarma yetkiniz yok.";
                return RedirectToAction("Details", new { id = vm.TaskId });
            }

            // --- YENİ KURAL: Hatalıysa, Departman/Kategori seçimi zorunlu ---
            if (task.Flag == FlagStatus.FlaggedAsIncorrect)
            {
                if (!vm.NewDepartmentId.HasValue || vm.NewDepartmentId == 0)
                    ModelState.AddModelError("NewDepartmentId", "Hatalı görev yönlendirilirken Departman seçimi zorunludur.");
                if (!vm.NewCategoryId.HasValue || vm.NewCategoryId == 0)
                    ModelState.AddModelError("NewCategoryId", "Hatalı görev yönlendirilirken Kategori seçimi zorunludur.");
            }
            // --- BİTTİ ---

            if (ModelState.IsValid)
            {
                var oldUser = await _repository.GetUserByIdAsync(task.AssigneeId);
                var newUser = await _repository.GetUserByIdAsync(vm.NewAssigneeId);
                string formattedComment = $"Aktarma Notu: {vm.Comment}";

                // Depo (repository) metodunun YENİ imzasını çağır
                await _repository.TransferTaskAsync(
                    vm.TaskId,
                    vm.NewAssigneeId,
                    vm.NewDepartmentId, // <-- YENİ
                    vm.NewCategoryId,   // <-- YENİ
                    loggedInUserId,
                    formattedComment
                );

                TempData["WorkLogSuccess"] = "Görev başarıyla aktarıldı ve Hata Bildirimi (varsa) temizlendi.";
                return RedirectToAction("Details", new { id = vm.TaskId });
            }

            // --- Hata Durumu (Form Geçersizse) (GÜNCELLENDİ) ---
            List<User> assignableUsers = new List<User>();
            if (task.Flag == FlagStatus.FlaggedAsIncorrect || loggedInUser.Role == UserRole.Manager)
            {
                assignableUsers = await _repository.GetUsersByDepartmentAsync(vm.NewDepartmentId); // Seçili departmana göre doldur
            }
            else
            {
                var departmentUsers = await _repository.GetUsersByDepartmentAsync(loggedInUser.DepartmentId);
                assignableUsers = departmentUsers.Where(u => u.Id != task.AssigneeId).ToList();
            }

            var userList = assignableUsers.Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.Id} - {u.FullName}"
            }).ToList();
            ViewBag.Users = new SelectList(userList, "Value", "Text", vm.NewAssigneeId);

            // Departman listesini de yeniden doldur
            ViewBag.Departments = new SelectList(await _repository.GetDepartmentsAsync(), "Id", "Name", vm.NewDepartmentId);

            return View(vm);
        }
        
        

        // --- HATALI GÖREV BİLDİRİMİ ---

        [HttpGet]
        public async Task<IActionResult> FlagTask(int id)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var task = await _repository.GetTaskByIdAsync(id);
            var loggedInUserId = GetCurrentUserId();

            if (task == null || task.AssigneeId != loggedInUserId)
            {
                TempData["WorkLogError"] = "Sadece size atanan bir görevi hatalı olarak bildirebilirsiniz.";
                return RedirectToAction("Details", new { id = id });
            }
            
            var vm = new FlagTaskViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagTask(FlagTaskViewModel vm)
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return sessionCheck;

            var task = await _repository.GetTaskByIdAsync(vm.TaskId);
            var loggedInUserId = GetCurrentUserId();

            if (task == null || task.AssigneeId != loggedInUserId)
            {
                TempData["WorkLogError"] = "Yetkisiz işlem.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                await _repository.FlagTaskAsync(vm.TaskId, loggedInUserId, vm.FlagComment);
                TempData["WorkLogSuccess"] = "Görev 'Hatalı' olarak işaretlendi ve yöneticinize bildirildi.";
                return RedirectToAction("Details", new { id = vm.TaskId });
            }

            return View(vm);
        }

        // --- JSON API METOTLARI (JavaScript için) ---

        [HttpGet]
        public async Task<JsonResult> GetCategories(int departmentId) 
        { 
            var categories = await _repository.GetCategoriesByDepartmentAsync(departmentId);
            return Json(new SelectList(categories, "Id", "Name"));
        }

        [HttpGet]
        public async Task<JsonResult> GetResponsibleUser(int categoryId) 
        { 
            var category = await _repository.GetCategoryByIdAsync(categoryId);
            
            if (category != null && category.ResponsibleUserId.HasValue)
            {
                return Json(new { userId = category.ResponsibleUserId.Value });
            }
            
            return Json(new { userId = 0 });
        }

        [HttpGet]
        public async Task<JsonResult> GetUsersForDepartment(int? departmentId)
        {
            var users = await _repository.GetUsersByDepartmentAsync(departmentId);

            var userList = users.Select(u => new
            {
                value = u.Id.ToString(),
                text = $"{u.Id} - {u.FullName}"
            }).ToList();

            return Json(userList);
        }
        [HttpGet]
        public async Task<IActionResult> DownloadReport()
        {
            var sessionCheck = CheckSession();
            if (sessionCheck != null) return File(new byte[0], "application/pdf"); // Boş dosya dön

            var loggedInUserId = GetCurrentUserId();
            var loggedInUser = await _repository.GetUserByIdAsync(loggedInUserId);

            if (loggedInUser == null || loggedInUser.Role == UserRole.Employee)
            {
                // Çalışanlar rapor alamaz
                TempData["WorkLogError"] = "Rapor alma yetkiniz yok.";
                return RedirectToAction("Index");
            }
            
            // 1. Rapor için gerekli TÜM verileri hazırla
            var reportModel = new ReportViewModel
            {
                ReportingUser = loggedInUser,
                // Rapor, kullanıcının hiyerarşik panelindeki görevleri içermeli
                Tasks = await _repository.GetTasksForUserAsync(loggedInUserId), 
                // ID'leri isme çevirmek için tüm yardımcı listeler
                AllUsers = await _repository.GetUsersAsync(),
                AllDepartments = await _repository.GetDepartmentsAsync(),
                AllCategories = await _repository.GetAllCategoriesAsync()
            };

            // 2. Rapor nesnesini oluştur
            var report = new HierarchicalTaskApp.Reports.TeamTaskReport(reportModel);

            // 3. PDF'i byte dizisi olarak oluştur
            // Not: .GeneratePdf() senkron bir metottur
            byte[] pdfBytes = report.GeneratePdf();

            // 4. PDF'i 'File' olarak kullanıcıya döndür
            string fileName = $"Ekip_Raporu_{DateTime.Now.ToString("yyyyMMdd_HHmm")}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}