using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class FlagTaskViewModel
    {
        public int TaskId { get; set; }
        
        [Display(Name = "Görev Başlığı")]
        public string TaskTitle { get; set; } = string.Empty;

        [Display(Name = "Hata Bildirim Notu")]
        [Required(ErrorMessage = "Lütfen görevin neden hatalı olduğunu açıklayın.")]
        [DataType(DataType.MultilineText)]
        public string FlagComment { get; set; } = string.Empty;
    }
}