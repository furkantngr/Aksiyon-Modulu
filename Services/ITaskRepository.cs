using HierarchicalTaskApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HierarchicalTaskApp.Services
{
    public interface ITaskRepository
    {
        Task<List<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<ActionTask?> GetTaskByIdAsync(int taskId);
        Task<List<ActionTask>> GetTasksForUserAsync(int loggedInUserId);
        Task CreateTaskAsync(ActionTask task);
        Task<List<TaskHistory>> GetHistoryForTaskAsync(int taskId);
        Task UpdateTaskStatusAsync(int taskId, int loggedInUserId, HierarchicalTaskApp.Models.TaskStatus newStatus, string comment);
        Task<List<TaskComment>> GetCommentsForTaskAsync(int taskId);
        Task AddTaskCommentAsync(TaskComment comment);
        Task<List<ActionTask>> GetTasksAssignedByUserAsync(int assignerId);
        Task<List<ActionTask>> GetTasksAssignedToUserAsync(int assigneeId);
        Task<List<WorkLog>> GetWorkLogsForTaskAsync(int taskId);
        Task AddWorkLogAsync(WorkLog log);
        Task<List<Department>> GetDepartmentsAsync();
        Task<List<Category>> GetCategoriesByDepartmentAsync(int departmentId);
        Task<Category?> GetCategoryByIdAsync(int categoryId);
        Task<List<User>> GetUsersByDepartmentAsync(int? departmentId);
        Task<List<Category>> GetAllCategoriesAsync();
        Task UpdateTaskPriorityAsync(int taskId, int actorUserId, TaskPriority newPriority, TaskPriority oldPriority);
        Task FlagTaskAsync(int taskId, int actorUserId, string flagComment);
        Task<bool> IsUserSubordinateOfAsync(int potentialSubordinateId, int managerId);

        // --- HATA BURADAYDI ---
        // Bu, metodun en güncel ve doğru imzasıdır:
        Task TransferTaskAsync(int taskId, int newAssigneeId, int? newDepartmentId, int? newCategoryId, int actorUserId, string comment);
    }
}