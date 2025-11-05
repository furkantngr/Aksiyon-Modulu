using System.Text.Json.Serialization; // [JsonIgnore] için eklendi

namespace HierarchicalTaskApp.Models
{
    public class User
    {
        public int Id { get; set; }
        
        // 'Name' alanı kaldırıldı, yerine bunlar eklendi:
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        public UserRole Role { get; set; }
        
        public int? ManagerId { get; set; }
        public int? DepartmentId { get; set; }

        // --- YENİ YARDIMCI ÖZELLİK ---
        // Bu özellik JSON'a kaydedilmez, sadece kod içinde kullanılır
        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}