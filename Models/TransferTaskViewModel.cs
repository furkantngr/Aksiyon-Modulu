using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class TransferTaskViewModel
    {
        public int TaskId { get; set; }
        
        [Display(Name = "Görev Başlığı")]
        public string TaskTitle { get; set; } = string.Empty;
        
        // Bu görevi kime aktaracağız?
        [Display(Name = "Yeni Atanan Kişi")]
        [Required(ErrorMessage = "Lütfen görevi aktaracağınız kişiyi seçin.")]
        public int NewAssigneeId { get; set; }

        [Display(Name = "Aktarma Notu")]
        [Required(ErrorMessage = "Lütfen aktarma için bir sebep/not girin.")]
        [DataType(DataType.MultilineText)]
        public string Comment { get; set; } = string.Empty;

        // --- YENİ EKLENDİ ---
        
        // Bu özellik, View'a Departman/Kategori alanlarını
        // gösterip göstermeyeceğini söyler.
        public bool IsFlagged { get; set; } 

        [Display(Name = "Yeni Departman")]
        public int? NewDepartmentId { get; set; } // Hatalıysa doldurulacak

        [Display(Name = "Yeni Kategori")]
        public int? NewCategoryId { get; set; } // Hatalıysa doldurulacak
    }
}