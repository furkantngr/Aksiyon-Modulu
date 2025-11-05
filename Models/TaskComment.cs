using System;
using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class TaskComment
    {
        public int Id { get; set; }
        public int ActionTaskId { get; set; } // Hangi göreve ait
        public int AuthorUserId { get; set; } // Yorumu kim yazdı
        
        [Required(ErrorMessage = "Yorum alanı boş bırakılamaz.")]
        [Display(Name = "Yorumunuz")]
        public string CommentText { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } // Yorum tarihi
    }
}