using Org.BouncyCastle.Bcpg.OpenPgp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace ReimbursementProject.Models
{
    public class EmployeeImpress
    {
        [Key]
        public long ID { get; set; }

        [StringLength(50)]
        public string? EmpID { get; set; } = null!; // non-nullable with default assignment

       
        [StringLength(100)]
        public string EmpName { get; set; } = null!;

        public double? IMPRESS_AMOUNT { get; set; } // Nullable, matches float

        public double? ADVANCE_AMOUNT { get; set; } // Nullable, matches float

        [StringLength(500)]
        public string? REMARK { get; set; }

        [StringLength(50)]
        public string? STATUS { get; set; }
        public DateTime? DATETIME { get; set; }
        public string? PROJECT_DETAILS { get; set; }
        public string? MODE { get; set; }

    }
}
