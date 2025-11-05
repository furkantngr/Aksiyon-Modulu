using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public enum TaskPriority
    {
        [Display(Name = "Düşük")]
        Low = 0,
        
        [Display(Name = "Normal")]
        Normal = 1,
        
        [Display(Name = "Yüksek")]
        High = 2,
        
        [Display(Name = "Acil")]
        Urgent = 3
    }
}