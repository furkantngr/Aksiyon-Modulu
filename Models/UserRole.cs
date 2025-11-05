using System.ComponentModel.DataAnnotations; // <-- YENİ EKLENDİ

namespace HierarchicalTaskApp.Models
{
    public enum UserRole
    {
        [Display(Name = "Yönetici")] // DEĞİŞTİ
        Manager,
        
        [Display(Name = "Takım Lideri")] // DEĞİŞTİ
        TeamLeader,
        
        [Display(Name = "Çalışan")] // DEĞİŞTİ
        Employee
    }
}