using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;
using Microsoft.AspNetCore.Authorization;

namespace locationget.Controllers
{
    //[Authorize(AuthenticationSchemes = "EmployeeAuth")]
    public class HomeController : Controller
    {
        public readonly ApplicationDbContext _context;
        private readonly StoreAppDbContext _storecontext;

        public HomeController(ApplicationDbContext context,
        StoreAppDbContext storecontext)
        {
            _context = context;
            _storecontext = storecontext;
        }
        // GET: Home/Index

        public IActionResult Index()
        {
            
           
            var pendingCount = _context.ExpenseLogBook
                .Where(e => e.Status == "0")  // pending rows only
                .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode })
                .Count();

            ViewBag.PendingCount = pendingCount;

            return View();
        }

        public IActionResult ExpenseReport()
        {
            return View();
        }
        // POST: ExpenseReport/Filter
      
        [HttpPost]
        public async Task<IActionResult> Filter(DateTime? fromDate, DateTime? toDate)
        {
            // Start with all records
            var query = _context.ExpenseLogBook.AsQueryable();

            // Filter by date range if provided
            if (fromDate.HasValue)
                query = query.Where(e => e.DateofExpense >= fromDate.Value&& e.Rejection!="reject");

            if (toDate.HasValue)
                query = query.Where(e => e.DateofExpense <= toDate.Value&& e.Rejection != "reject");

            // Order by EmpID
            var data = await query
                .OrderBy(e => e.EmpID)
                .ThenBy(e => e.DateofExpense) // optional: secondary sort by date
                .ToListAsync();

            return View("ExpenseReport", data);
        }

        //[HttpGet]
        //public IActionResult Details(string type)
        //{
        //    // Pass type (pending, approved, rejected) to the view
        //    ViewBag.Type = type;
        //    return View();
        //}
        [HttpGet]
        public IActionResult ProjectExpense()
        {
            return View(); // This will load Views/Account/Login.cshtml
        }
        [HttpGet]
        public IActionResult EmployeePendingAmount()
        {
            return View(); // This will load Views/Account/Login.cshtml
        }
        [HttpGet]
        public IActionResult Login()
        {
            TempData["success"] = "login the user";
            return View(); // This will load Views/Account/Login.cshtml
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View(); // This will load Views/Account/Login.cshtml
        }
        [HttpGet]
        public IActionResult GroupedList()
        {
            return View();
        }
        // Views/Expense/GroupedList.cshtml
        [HttpGet]
        public IActionResult Details(string empid, string site, string project, DateTime submissionDate)
        {
            // The view reads query params and calls API to fetch data
            return View();
        }

        [HttpGet]
        public IActionResult NewEmployees()
        {
            return View();
        }

        [HttpGet]
        public IActionResult EmployeeList()
        {
            return View();
        }


        [HttpGet]
        public IActionResult Dashboard()
        {
            return View(); // This will load Views/Account/Login.cshtml
        }

        //prifile controller
        [Authorize]
        public IActionResult Profile()
        {
            var empId = User.FindFirst("EmpID")?.Value;

            if (string.IsNullOrEmpty(empId))
                return RedirectToAction("Login");

            var profile = _context.EmployeeDetails
                .FirstOrDefault(e => e.EmpID == empId);

            if (profile == null)
                return NotFound();

            return View(profile);
        }
        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Password cannot be empty";
                return RedirectToAction("Profile");
            }

            var empId = User.FindFirst("EmpID")?.Value;

            var employee = _context.EmployeeDetails
                .FirstOrDefault(e => e.EmpID == empId);
            var storeEmployee = _storecontext.Employees.FirstOrDefault(e => e.UserName == empId);

            if (employee == null)
                return NotFound();
          

            // 🔐 Ideally hash the password
            employee.Password = newPassword;
            storeEmployee.Password=newPassword;
          

            _context.SaveChanges();
            _storecontext.SaveChanges();

            TempData["Success"] = "Password updated successfully";
            return RedirectToAction("Profile");
        }




        //[HttpGet]
        //public IActionResult PendingDetails()
        //{
        //    var pendingDetails = _context.ExpenseLogBook
        //        .Where(e => e.Status == "0")
        //        .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode, e.SiteName, e.IRB })
        //        .Select(g => new
        //        {
        //            EmpID = g.Key.EmpID,
        //            EmpName = _context.EmployeeDetails
        //                              .Where(emp => emp.EmpID == g.Key.EmpID)
        //                              .Select(emp => emp.EmpName)
        //                              .FirstOrDefault(),

        //            SiteName = g.Key.SiteName,
        //            ProjectCode = g.Key.ProjectCode,
        //            SubmissionDate = g.Key.SubmissionDate,
        //            IRB = g.Key.IRB,

        //            IRBName = _context.EmployeeDetails
        //                              .Where(emp => emp.EmpID == g.Key.IRB)
        //                              .Select(emp => emp.EmpName)
        //                              .FirstOrDefault(),

        //            Count = g.Count()
        //        })
        //        .ToList();

        //    return View(pendingDetails);
        //}


        //[HttpGet]
        //public IActionResult ExpenseDetails(string empId, string siteName, string projectCode, DateTime submissionDate, string irb)
        //{
        //    var details = _context.ExpenseLogBook
        //        .Where(e => e.Status == "0"
        //                 && e.EmpID == empId
        //                 && e.SiteName == siteName
        //                 && e.ProjectCode == projectCode
        //                 && e.SubmissionDate == submissionDate
        //                 && e.IRB == irb)
        //        .ToList();

        //    return Json(details);
        //}


        //[HttpPost]
        //public IActionResult UpdateExpenses([FromBody] List<ExpenseLogBook> updates)
        //{
        //    foreach (var upd in updates)
        //    {
        //        var exp = _context.ExpenseLogBook.FirstOrDefault(e => e.ID == upd.ID);
        //        if (exp != null)
        //        {
        //            if (upd.SanctionedAmount != null)
        //                exp.SanctionedAmount = upd.SanctionedAmount;

        //            // Update Status (increment by 1 if numeric, else set to new string)
        //            if (int.TryParse(exp.Status, out int s))
        //                exp.Status = (s + 1).ToString();
        //            else
        //                exp.Status = upd.Status;
        //        }
        //    }

        //    _context.SaveChanges();
        //    return Ok();
        //}

        //public IActionResult Edit(long id)
        //     {

        //         var expense11 = _context.ExpenseLogBook.FirstOrDefault(e => e.ExpenseID == id);


        //         return View(expense11);
        //     }

        public IActionResult Edit(long id)
        {
            // Get all rows for the given ExpenseID
            var expenses = _context.ExpenseLogBook
                            .Where(e => e.ExpenseID == id)
                            .ToList();

            if (!expenses.Any()) return NotFound();

            return View(expenses); // pass list to view
        }

        // POST: Home/Edit/5
        [HttpPost]
       
        public IActionResult Edit(List<ExpenseLogBook> updatedExpenses)
        {
            if (updatedExpenses == null || !updatedExpenses.Any())
                return BadRequest();

            foreach (var updated in updatedExpenses)
            {
                var existing = _context.ExpenseLogBook.FirstOrDefault(e => e.ID == updated.ID);
                if (existing != null)
                {
                    existing.TypeOfExpense = updated.TypeOfExpense;
                    existing.Quantity = updated.Quantity;
                    existing.FellowMembers = updated.FellowMembers;
                    existing.ClaimAmount = updated.ClaimAmount;
                    existing.SiteName = updated.SiteName;
                    existing.ProjectCode = updated.ProjectCode;
                    existing.SubmissionDate = DateTime.Now;

                    // Update bill document if new file uploaded
                    if (updated.BillDocument != null && updated.BillDocument.Length > 0)
                    {
                        existing.BillDocument = updated.BillDocument;
                        existing.BillFileName = updated.BillFileName;
                        existing.BillContentType = updated.BillContentType;
                    }
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Dashboard");
        }




    }
}
