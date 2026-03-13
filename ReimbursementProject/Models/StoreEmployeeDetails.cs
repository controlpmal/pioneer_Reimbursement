using System.ComponentModel.DataAnnotations;

namespace ReimbursementProject.Models
{
    public class StoreEmployeeDetails
    {
        [Key]
        public int EmployeeId { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public string? Role { get; set; } = "Employee";
        public string? IRB { get; set; }
        public string? Level { get; set; }
    }
}
