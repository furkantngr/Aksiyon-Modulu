using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class UpdateStatusViewModel
    {
        public int TaskId { get; set; }
        
        [Display(Name = "Görev Başlığı")] // DEĞİŞTİ
        public string TaskTitle { get; set; } = string.Empty;

        [Display(Name = "Mevcut Durum")] // DEĞİŞTİ
        public TaskStatus CurrentStatus { get; set; }

        [Display(Name = "Yeni Durum")] // DEĞİŞTİ
        public TaskStatus NewStatus { get; set; }

        [Required(ErrorMessage = "Lütfen durum değişikliği için bir yorum girin.")]
        [Display(Name = "Güncelleme Yorumu")] // DEĞİŞTİ
        [DataType(DataType.MultilineText)]
        public string Comment { get; set; } = string.Empty;
    }
}