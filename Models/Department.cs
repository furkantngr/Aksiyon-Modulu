using System.ComponentModel.DataAnnotations;

namespace HierarchicalTaskApp.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}