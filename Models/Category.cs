using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        // Hangi departmana bağlı
        public int DepartmentId { get; set; }
        
        // Bu kategoriden sorumlu kişi (isteğe bağlı, null olabilir)
        public int? ResponsibleUserId { get; set; }
    }
}