using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Models;



namespace ReimbursementProject.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<EmployeeDetails> EmployeeDetails { get; set; } 
        public DbSet<ExpenseLimitDetails> ExpenseLimitDetails { get; set; }
        public DbSet<ExpenseLogBook>ExpenseLogBook { get; set; }
        public DbSet<TypeOfExpenses>TypeOfExpense { get; set; }
        public DbSet<ExpenseBillBook> ExpenseBillBook { get; set; }
        public DbSet<EmployeeAccount> EmployeeAccount { get; set; } 
        public DbSet<EmployeeImpress> EmployeeImpress { get; set; }
        public DbSet<Notification> Notifications { get; set; }

    }
    public class StoreAppDbContext : DbContext
    {
        public StoreAppDbContext(DbContextOptions<StoreAppDbContext> options) : base(options)
        {
        }

        public DbSet<StoreEmployeeDetails> Employees { get; set; }
    }
}
