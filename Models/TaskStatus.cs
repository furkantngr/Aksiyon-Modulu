using System.ComponentModel.DataAnnotations; // <-- YENİ EKLENDİ

namespace HierarchicalTaskApp.Models
{
    public enum TaskStatus
    {
        [Display(Name = "Beklemede")] // DEĞİŞTİ
        Todo,
        
        [Display(Name = "Devam Ediyor")] // DEĞİŞTİ
        InProgress,
        
        [Display(Name = "Tamamlandı")] // DEĞİŞTİ
        Done
    }
}