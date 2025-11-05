using System;
using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class WorkLog
    {
        public int Id { get; set; }
        public int ActionTaskId { get; set; } // Hangi göreve ait
        public int UserId { get; set; }       // Kaydı giren kullanıcı
        
        [Display(Name = "Tarih")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Lütfen bir tarih seçin.")]
        public DateTime LogDate { get; set; } // İşin yapıldığı tarih
        
        [Display(Name = "Harcanan Süre (Saat)")]
        [Range(0.5, 24, ErrorMessage = "Süre 0.5 ile 24 saat arasında olmalıdır.")]
        [Required(ErrorMessage = "Lütfen harcanan süreyi girin.")]
        public decimal HoursSpent { get; set; } // Harcanan saat (örn: 2.5)
        
        [Display(Name = "Çalışma Özeti")]
        [Required(ErrorMessage = "Lütfen ne yaptığınızı özetleyin.")]
        public string WorkSummary { get; set; } = string.Empty; // Ne yapıldığına dair kısa açıklama
    }
}