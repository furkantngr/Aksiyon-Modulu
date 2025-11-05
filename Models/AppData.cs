using HierarchicalTaskApp.Models;
using System.Collections.Generic;

namespace HierarchicalTaskApp.Models
{
    public class AppData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<ActionTask> Tasks { get; set; } = new List<ActionTask>();
        public List<TaskHistory> TaskHistories { get; set; } = new List<TaskHistory>();
        
        public List<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
        public List<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
        public List<Department> Departments { get; set; } = new List<Department>();
        public List<Category> Categories { get; set; } = new List<Category>();
    }
}