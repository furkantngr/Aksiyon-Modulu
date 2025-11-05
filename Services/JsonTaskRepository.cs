using HierarchicalTaskApp.Models;
using Microsoft.AspNetCore.Hosting; 
using System.Collections.Generic;
using System.IO; 
using System.Linq;
using System.Text.Json; 
using System.Text.Json.Serialization; 
using System.Text.Encodings.Web; 
using System.Text.Unicode; 
using System.Threading.Tasks;
using System;

namespace HierarchicalTaskApp.Services
{
    public class JsonTaskRepository : ITaskRepository
    {
        private readonly string _dbPath;
        private AppData _appData = null!; 
        
        private readonly JsonSerializerOptions _jsonOptions;
        private static readonly object _fileLock = new object();

        public JsonTaskRepository(IWebHostEnvironment webHostEnvironment)
        {
            _dbPath = Path.Combine(webHostEnvironment.ContentRootPath, "data", "db.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, 
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                Converters = { new JsonStringEnumConverter() }
            };

            LoadData();
        }

        private void LoadData()
        {
            lock (_fileLock)
            {
                if (!File.Exists(_dbPath))
                {
                    _appData = new AppData();
                    File.WriteAllText(_dbPath, JsonSerializer.Serialize(_appData, _jsonOptions));
                }
                else
                {
                    var json = File.ReadAllText(_dbPath);
                    _appData = JsonSerializer.Deserialize<AppData>(json, _jsonOptions) ?? new AppData();
                }
            }
        }

        private void SaveChanges()
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(_appData, _jsonOptions);
                File.WriteAllText(_dbPath, json);
            }
        }

        public Task<List<User>> GetUsersAsync()
        {
            return Task.FromResult(_appData.Users);
        }

        public Task<User?> GetUserByIdAsync(int userId)
        {
            var user = _appData.Users.FirstOrDefault(u => u.Id == userId);
            return Task.FromResult(user);
        }

        public Task<ActionTask?> GetTaskByIdAsync(int taskId)
        {
            var task = _appData.Tasks.FirstOrDefault(t => t.Id == taskId);
            return Task.FromResult(task);
        }

        public Task<List<TaskHistory>> GetHistoryForTaskAsync(int taskId)
        {
            var history = _appData.TaskHistories
                .Where(h => h.ActionTaskId == taskId)
                .OrderByDescending(h => h.ChangeDate)
                .ToList();
            return Task.FromResult(history);
        }

        public Task<List<ActionTask>> GetTasksForUserAsync(int loggedInUserId)
        {
            var loggedInUser = _appData.Users.FirstOrDefault(u => u.Id == loggedInUserId);
            if (loggedInUser == null)
            {
                return Task.FromResult(new List<ActionTask>());
            }

            var visibleTasks = new List<ActionTask>();
            visibleTasks.AddRange(_appData.Tasks.Where(t => t.AssigneeId == loggedInUserId));

            switch (loggedInUser.Role)
            {
                case UserRole.Employee:
                    break;
                case UserRole.TeamLeader:
                    var directReportIds = _appData.Users
                        .Where(u => u.ManagerId == loggedInUserId)
                        .Select(u => u.Id)
                        .ToList();
                    visibleTasks.AddRange(_appData.Tasks.Where(t => directReportIds.Contains(t.AssigneeId)));
                    break;
                case UserRole.Manager:
                    var allSubordinateIds = GetAllSubordinateIds(loggedInUserId);
                    visibleTasks.AddRange(_appData.Tasks.Where(t => allSubordinateIds.Contains(t.AssigneeId)));
                    break;
            }

            return Task.FromResult(visibleTasks.DistinctBy(t => t.Id).OrderBy(t => t.Status).ToList());
        }

        public Task CreateTaskAsync(ActionTask task)
        {
            task.Id = _appData.Tasks.Any() ? _appData.Tasks.Max(t => t.Id) + 1 : 1;
            _appData.Tasks.Add(task);

            var history = new TaskHistory
            {
                Id = _appData.TaskHistories.Any() ? _appData.TaskHistories.Max(h => h.Id) + 1 : 1,
                ActionTaskId = task.Id,
                UserId = task.AssignerId,
                OldStatus = null,
                NewStatus = HierarchicalTaskApp.Models.TaskStatus.Todo,
                Comment = "Görev Oluşturuldu.",
                ChangeDate = DateTime.Now
            };
            _appData.TaskHistories.Add(history);

            SaveChanges();
            return Task.CompletedTask;
        }

        public Task UpdateTaskStatusAsync(int taskId, int loggedInUserId, HierarchicalTaskApp.Models.TaskStatus newStatus, string comment)
        {
            var task = _appData.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                return Task.CompletedTask; 
            }

            var history = new TaskHistory
            {
                Id = _appData.TaskHistories.Any() ? _appData.TaskHistories.Max(h => h.Id) + 1 : 1,
                ActionTaskId = taskId,
                UserId = loggedInUserId,
                OldStatus = task.Status,
                NewStatus = newStatus, 
                Comment = comment,
                ChangeDate = DateTime.Now
            };
            _appData.TaskHistories.Add(history);

            task.Status = newStatus;

            SaveChanges();
            return Task.CompletedTask;
        }

        public Task<List<ActionTask>> GetTasksAssignedByUserAsync(int assignerId)
        {
            var tasks = _appData.Tasks
                .Where(t => t.AssignerId == assignerId)
                .OrderByDescending(t => t.Id)
                .ToList();
            return Task.FromResult(tasks);
        }

        public Task<List<TaskComment>> GetCommentsForTaskAsync(int taskId)
        {
            var comments = _appData.TaskComments
                .Where(c => c.ActionTaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ToList();
            return Task.FromResult(comments);
        }

        public Task AddTaskCommentAsync(TaskComment comment)
        {
            comment.Id = _appData.TaskComments.Any() ? _appData.TaskComments.Max(c => c.Id) + 1 : 1;
            comment.CreatedAt = DateTime.Now;
            _appData.TaskComments.Add(comment);
            
            SaveChanges();
            return Task.CompletedTask;
        }
        
        public Task<List<ActionTask>> GetTasksAssignedToUserAsync(int assigneeId)
        {
            var tasks = _appData.Tasks
                .Where(t => t.AssigneeId == assigneeId)
                .OrderBy(t => t.Status)
                .ToList();
            return Task.FromResult(tasks);
        }

        public Task<List<WorkLog>> GetWorkLogsForTaskAsync(int taskId)
        {
            var logs = _appData.WorkLogs
                .Where(l => l.ActionTaskId == taskId)
                .OrderByDescending(l => l.LogDate)
                .ToList();
            return Task.FromResult(logs);
        }

        public Task AddWorkLogAsync(WorkLog log)
        {
            log.Id = _appData.WorkLogs.Any() ? _appData.WorkLogs.Max(l => l.Id) + 1 : 1;
            _appData.WorkLogs.Add(log);
            SaveChanges();
            return Task.CompletedTask;
        }

        public Task<List<Department>> GetDepartmentsAsync()
        {
            return Task.FromResult(_appData.Departments.OrderBy(d => d.Name).ToList());
        }

        public Task<List<Category>> GetCategoriesByDepartmentAsync(int departmentId)
        {
            var categories = _appData.Categories
                .Where(c => c.DepartmentId == departmentId)
                .OrderBy(c => c.Name)
                .ToList();
            return Task.FromResult(categories);
        }

        public Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            var category = _appData.Categories.FirstOrDefault(c => c.Id == categoryId);
            return Task.FromResult(category);
        }
        
        public Task<List<Category>> GetAllCategoriesAsync()
        {
            return Task.FromResult(_appData.Categories);
        }

        public Task UpdateTaskPriorityAsync(int taskId, int actorUserId, TaskPriority newPriority, TaskPriority oldPriority)
        {
            var task = _appData.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                return Task.CompletedTask;
            }

            task.Priority = newPriority;
            var historyComment = $"Görev önceliği '{oldPriority}' seviyesinden '{newPriority}' seviyesine değiştirildi.";
            
            var history = new TaskHistory
            {
                Id = _appData.TaskHistories.Any() ? _appData.TaskHistories.Max(h => h.Id) + 1 : 1,
                ActionTaskId = taskId,
                UserId = actorUserId,
                OldStatus = task.Status,
                NewStatus = task.Status,
                Comment = historyComment,
                ChangeDate = DateTime.Now
            };
            _appData.TaskHistories.Add(history);

            SaveChanges();
            return Task.CompletedTask;
        }

        public Task FlagTaskAsync(int taskId, int actorUserId, string flagComment)
        {
            var task = _appData.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                return Task.CompletedTask;
            }

            task.Flag = FlagStatus.FlaggedAsIncorrect;

            // .Name -> .FullName
            var actorName = GetUserByIdAsync(actorUserId).Result?.FullName ?? "Bilinmeyen Kullanıcı";
            var historyComment = $"Görev, '{actorName}' tarafından HATALI olarak işaretlendi.\nBildirim Notu: {flagComment}";
            
            var history = new TaskHistory
            {
                Id = _appData.TaskHistories.Any() ? _appData.TaskHistories.Max(h => h.Id) + 1 : 1,
                ActionTaskId = taskId,
                UserId = actorUserId,
                OldStatus = task.Status,
                NewStatus = task.Status,
                Comment = historyComment,
                ChangeDate = DateTime.Now
            };
            _appData.TaskHistories.Add(history);

            SaveChanges();
            return Task.CompletedTask;
        }

        public Task TransferTaskAsync(int taskId, int newAssigneeId, int? newDepartmentId, int? newCategoryId, int actorUserId, string comment)
        {
            var task = _appData.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                return Task.CompletedTask;
            }

            // Loglama için gerekli isimleri al
            var oldAssigneeId = task.AssigneeId;
            var oldUser = _appData.Users.FirstOrDefault(u => u.Id == oldAssigneeId)?.FullName ?? "Bilinmeyen";
            var newUser = _appData.Users.FirstOrDefault(u => u.Id == newAssigneeId)?.FullName ?? "Bilinmeyen";
            var actorUser = _appData.Users.FirstOrDefault(u => u.Id == actorUserId)?.FullName ?? "Bilinmeyen";

            // 1. Görev atamasını güncelle
            task.AssigneeId = newAssigneeId;
            
            // 2. Görev aktarıldığı için (yönlendirildiği için) hata bayrağını temizle
            task.Flag = FlagStatus.None;

            // 3. (YENİ) Eğer yeni departman/kategori seçildiyse, onları da güncelle
            string deptChangeComment = "";
            if (newDepartmentId.HasValue && newDepartmentId.Value > 0)
            {
                task.DepartmentId = newDepartmentId.Value;
                
                // Kategori de (muhtemelen) yeni departmana ait olmalı
                if(newCategoryId.HasValue && newCategoryId.Value > 0)
                {
                    task.CategoryId = newCategoryId.Value;
                }
                
                // Log için departman adını al
                var newDeptName = _appData.Departments.FirstOrDefault(d => d.Id == newDepartmentId.Value)?.Name ?? "?";
                deptChangeComment = $"\nGörev, '{newDeptName}' departmanına yeniden yönlendirildi.";
            }

            // 4. Değişikliği TaskHistory'ye logla
            var historyComment = $"Görev, '{actorUser}' tarafından '{oldUser}' kullanıcısından '{newUser}' kullanıcısına aktarıldı.\nAktarma Notu: {comment}{deptChangeComment}";
            
            var history = new TaskHistory
            {
                Id = _appData.TaskHistories.Any() ? _appData.TaskHistories.Max(h => h.Id) + 1 : 1,
                ActionTaskId = taskId,
                UserId = actorUserId,
                OldStatus = task.Status, // Durum değişmiyor
                NewStatus = task.Status,
                Comment = historyComment,
                ChangeDate = DateTime.Now
            };
            _appData.TaskHistories.Add(history);

            // 5. Değişiklikleri JSON'a kaydet
            SaveChanges();
            return Task.CompletedTask;
        }

        // --- DÜZELTME BURADA (GetUsersByDepartmentAsync) ---
        public Task<List<User>> GetUsersByDepartmentAsync(int? departmentId)
        {
            if (!departmentId.HasValue || departmentId.Value == 0)
            {
                // HATA BURADAYDI: .OrderBy(u => u.Name) -> .OrderBy(u => u.LastName)
                return Task.FromResult(_appData.Users.OrderBy(u => u.LastName).ToList());
            }

            // HATA BURADAYDI: .OrderBy(u => u.Name) -> .OrderBy(u => u.LastName)
            var users = _appData.Users
                .Where(u => u.DepartmentId == departmentId.Value || u.DepartmentId == null)
                .OrderBy(u => u.LastName) 
                .ToList();
                
            return Task.FromResult(users);
        }

        private List<int> GetAllSubordinateIds(int managerId)
        {
            var allIds = new List<int>();
            var directReports = _appData.Users.Where(u => u.ManagerId == managerId).ToList();

            foreach (var report in directReports)
            {
                allIds.Add(report.Id);
                allIds.AddRange(GetAllSubordinateIds(report.Id));
            }

            return allIds;
        }
        public Task<bool> IsUserSubordinateOfAsync(int potentialSubordinateId, int managerId)
        {
            // O yöneticinin altındaki tüm (doğrudan veya dolaylı) ID'leri al
            var allSubordinateIds = GetAllSubordinateIds(managerId);
            
            // Verilen 'potentialSubordinateId', bu listede var mı?
            return Task.FromResult(allSubordinateIds.Contains(potentialSubordinateId));
        }
    }
}