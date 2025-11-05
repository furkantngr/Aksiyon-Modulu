using HierarchicalTaskApp.Models; // TaskPriority için eklendi
using System.Collections.Generic;

namespace HierarchicalTaskApp.Models
{
    public class TaskDashboardViewModel
    {
        public UserRole LoggedInUserRole { get; set; }
        public List<ActionTask> HierarchicalTasks { get; set; } = new List<ActionTask>();
        public List<ActionTask> TasksAssignedByMe { get; set; } = new List<ActionTask>();
        public List<ActionTask> TasksAssignedToMe { get; set; } = new List<ActionTask>();

        // --- YENİ EKLENDİ (PASTA GRAFİK İÇİN) ---
        public List<string> ChartLabels { get; set; } = new List<string>();
        public List<int> ChartData { get; set; } = new List<int>();
    }
}