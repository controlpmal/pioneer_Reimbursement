using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ReimbursementProject.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }  // Auto-incremented primary key

        [Required]
        [StringLength(20)]
        public string? EmpID { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Message { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }  // e.g., Expense, Approval, Login

        public bool? IsRead { get; set; } = false;  // default false

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
