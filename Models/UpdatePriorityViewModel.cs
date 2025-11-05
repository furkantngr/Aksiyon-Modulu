using HierarchicalTaskApp.Models;
using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class UpdatePriorityViewModel
    {
        public int TaskId { get; set; }
        
        [Display(Name = "Görev Başlığı")]
        public string TaskTitle { get; set; } = string.Empty;

        [Display(Name = "Mevcut Öncelik")]
        public TaskPriority CurrentPriority { get; set; }

        [Display(Name = "Yeni Öncelik")]
        [Required(ErrorMessage = "Lütfen yeni bir öncelik seviyesi seçin.")]
        public TaskPriority NewPriority { get; set; }
    }
}