using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class TaskHistory
    {
        public int Id { get; set; }
        public int ActionTaskId { get; set; }
        public int UserId { get; set; } // Değişikliği yapan kullanıcı
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus NewStatus { get; set; }
        
        [Required]
        public string Comment { get; set; } = string.Empty;
        
        public DateTime ChangeDate { get; set; }
    }
}