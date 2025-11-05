using HierarchicalTaskApp.Models;
using System.Collections.Generic;

namespace HierarchicalTaskApp.Models
{
    // Bu model, PDF oluşturucuya gereken tüm verileri taşır
    public class ReportViewModel
    {
        // Raporu hangi kullanıcının istediği (Örn: "Ahmet Kaya (Teknik Müdür)")
        public User ReportingUser { get; set; } = new User();

        // Rapora dahil edilecek ana görev listesi
        public List<ActionTask> Tasks { get; set; } = new List<ActionTask>();
        
        // ID'leri isme çevirmek için yardımcı listeler
        public List<User> AllUsers { get; set; } = new List<User>();
        public List<Department> AllDepartments { get; set; } = new List<Department>();
        public List<Category> AllCategories { get; set; } = new List<Category>();
    }
}