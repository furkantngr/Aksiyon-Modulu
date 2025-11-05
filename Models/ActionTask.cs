using System;
using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class ActionTask
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Lütfen görev için bir açıklama girin.")]
        [Display(Name = "Açıklama")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        public int AssignerId { get; set; } 

        [Display(Name = "Atanan Kişi")]
        [Required]
        public int AssigneeId { get; set; } 

        public TaskStatus Status { get; set; }

        [Display(Name = "Son Teslim Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; } 
        
        [Display(Name = "Departman")]
        [Required(ErrorMessage = "Lütfen bir departman seçin.")]
        public int? DepartmentId { get; set; }
        
        [Display(Name = "Kategori")]
        [Required(ErrorMessage = "Lütfen bir kategori seçin.")]
        public int? CategoryId { get; set; }

        // --- YENİ EKLENDİ ---
        [Display(Name = "Öncelik")]
        public TaskPriority Priority { get; set; } = TaskPriority.Normal;

        [Display(Name = "Hata Durumu")]
        public FlagStatus Flag { get; set; } = FlagStatus.None;
    }
}