using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace ReimbursementProject.Controllers
{
    public class ExpenseEditController : Controller

    {
        private readonly ApplicationDbContext _context;
        public ExpenseEditController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EditSingle(long id)
        {
            var expenseItem = await _context.ExpenseLogBook
                .FirstOrDefaultAsync(e => e.ID == id);
            if (expenseItem.Rejection == "reject" || expenseItem.Status == "5" || expenseItem.Status == "0")
            {

            }
            else
            {
                return NotFound();
            }
            var level = User.FindFirst("Level")?.Value;

            var expenseamount = await _context.ExpenseLimitDetails.Where(e => e.Level == level).ToListAsync();

            if (expenseItem == null)
                return NotFound();

            // Employee Details
            var employees = await _context.EmployeeDetails
      .Select(e => new
      {
          e.EmpID,
          e.EmpName
      })
      .ToListAsync();

            ;

            // Expense Type list
            var types = await _context.TypeOfExpense.ToListAsync();

            // ViewBags
            ViewBag.ExpenseAmount = expenseamount;
            ViewBag.Employee = employees;
            ViewBag.Types = types;
            ViewBag.ProjectCode = expenseItem.ProjectCode;
            ViewBag.SubmissionDate = expenseItem.SubmissionDate;
            ViewBag.ExpenseID = expenseItem.ExpenseID;
            ViewBag.IRB = expenseItem.IRB;
            ViewBag.SiteName = expenseItem.SiteName;
            ViewBag.EmpID = expenseItem.EmpID;

            return View("GroupEdit", expenseItem);   // ✅ return ONE model
        }


        [HttpPost]
        [Route("api/expenseedit/update/{id}/{km}")]
        public async Task<IActionResult> Update(long id, long km, [FromForm] ExpenseLogBook model, IFormFile? BillDocument,
                                        [FromForm] string? RemoveOldFile)
        {

            var km1 = km;
            var level = User.FindFirst("Level")?.Value;
            var entity = await _context.ExpenseLogBook.FirstOrDefaultAsync(x => x.ID == id);
            if (entity == null)
                return NotFound();
            if (entity.Rejection == "reject" || entity.Status == "5" || entity.Status == "0")
            {
                if (entity.Status != "0" && entity.Status != "5")
                {
                    entity.Status = "1";
                }
            }
            else
            {
                return NotFound();
            }

            // normal updates
            entity.TypeOfExpense = model.TypeOfExpense;
            entity.TravelLocation = model.TravelLocation;
            entity.Quantity = model.Quantity;
            entity.FellowMembers = model.FellowMembers;
            entity.ClaimAmount = model.ClaimAmount;
            //entity.SanctionedAmount = (model.ClaimAmount > (model.SanctionedAmount ?? 0)) ? model.SanctionedAmount : model.ClaimAmount;
            entity.DateofExpense = model.DateofExpense;
            //entity.Rejection = null;
            //entity.Reason = null;

            // ✅ Compute sanctioned amount
            double computedSanctioned = 0;
            var limitRow = _context.ExpenseLimitDetails
                                   .FirstOrDefault(x => x.Level == level && x.TypeOfExpense == model.TypeOfExpense);
            double maxLimit = 0;
            //bool hasBill = BillDocument != null || RemoveOldFile!="true";
            if (limitRow != null)
            {
                maxLimit = (BillDocument != null)||RemoveOldFile=="false"
                    ? (limitRow.MaxLimitWithBill ?? 0)
                    : (limitRow.MaxLimitWOBill ?? 0);
            }

            bool isTravel = (model.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL BIKE")
                            || (model.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL CAR");
            bool isHotel = (model.TypeOfExpense ?? "").ToUpper().Contains("HOTEL ROOM/DAY");
            if (isTravel) computedSanctioned = (km) * maxLimit;
            else if (isHotel) computedSanctioned = maxLimit;
            else computedSanctioned = (model.Quantity ?? 0) * maxLimit;
            int computedSanctionedInt = (int)Math.Floor(computedSanctioned);
            if (computedSanctionedInt == null)
            {
                computedSanctionedInt = 0;
            }
            entity.SanctionedAmount = (model.ClaimAmount > (computedSanctionedInt))
                                    ? computedSanctionedInt
                                    : model.ClaimAmount;
            entity.RequireSpecialApproval = (model.ClaimAmount > (computedSanctionedInt)) ? "true" : "false";



            // remove file if requested
            if (RemoveOldFile == "true")
            {
                entity.BillDocument = null;
                entity.BillFileName = null;
                entity.BillContentType = null;
            }

            // upload if new file added
            if (BillDocument != null)
            {
                using (var ms = new MemoryStream())
                {
                    await BillDocument.CopyToAsync(ms);
                    entity.BillDocument = ms.ToArray();
                }

                entity.BillFileName = BillDocument.FileName;
                entity.BillContentType = BillDocument.ContentType;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated Successfully" });
        }

        //new expnnse adding through eidt view page
        [HttpPost]
        [Route("api/expenseedit/createnew/{km}")]
        public async Task<IActionResult> AddNew(long km, [FromForm] ExpenseLogBook model, IFormFile BillDocument)
        {
            var level = User.FindFirst("Level")?.Value;
            if (BillDocument != null)
            {
                using var ms = new MemoryStream();
                await BillDocument.CopyToAsync(ms);
                model.BillDocument = ms.ToArray();
                model.BillFileName = BillDocument.FileName;
                model.BillContentType = BillDocument.ContentType;
            }

            model.Status = "5"; // or default
                                // ✅ Compute sanctioned amount
            double computedSanctioned = 0;
            var limitRow = _context.ExpenseLimitDetails
                                   .FirstOrDefault(x => x.Level == level && x.TypeOfExpense == model.TypeOfExpense);
            double maxLimit = 0;
            if (limitRow != null)
            {
                maxLimit = (BillDocument != null)
                    ? (limitRow.MaxLimitWithBill ?? 0)
                    : (limitRow.MaxLimitWOBill ?? 0);
            }

            bool isTravel = (model.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL BIKE")
                            || (model.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL CAR");
            bool isHotel = (model.TypeOfExpense ?? "").ToUpper().Contains("HOTEL ROOM/DAY");
            if (isTravel)
            {
                model.TravelLocation = $"{model.TravelLocation} - {km}KM";
                computedSanctioned = (km) * maxLimit;
            }
            else if (isHotel) computedSanctioned = maxLimit;
            else computedSanctioned = (model.Quantity ?? 0) * maxLimit;
            int computedSanctionedInt = (int)Math.Floor(computedSanctioned);
            if (computedSanctionedInt == null)
            {
                computedSanctionedInt = 0;
            }
            model.SanctionedAmount = (model.ClaimAmount > (computedSanctionedInt))
                                    ? computedSanctionedInt
                                    : model.ClaimAmount;
            model.RequireSpecialApproval = (model.ClaimAmount > (computedSanctionedInt)) ? "true" : "false";
         

            _context.ExpenseLogBook.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Inserted Successfully" });
        }

        // ---------------------------------------
        // CHANGE SITE NAME + PROJECT CODE (ALL ROWS WITH SAME ExpenseID)
        // ---------------------------------------
        [HttpPost]
        [Route("api/expenseedit/change/{expenseId}")]
        public async Task<IActionResult> Change(long expenseId, [FromBody] ChangeDTO model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var rows = await _context.ExpenseLogBook
                                     .Where(x => x.ExpenseID == expenseId)
                                     .ToListAsync();

            if (rows.Count == 0)
                return NotFound("No rows found for this ExpenseID");

            foreach (var row in rows)
            {
                row.SiteName = model.SiteName;
                row.ProjectCode = model.ProjectCode;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Project Code & Site Name updated successfully" });
        }







        //public async Task<IActionResult> GroupEdit(string empId, DateTime submissionDate, string projectCode, long expenseId)
        //{
        //    // 1️⃣ Fetch Employee Details
        //    var employee = await _context.EmployeeDetails
        //        .FirstOrDefaultAsync(e => e.EmpID == empId);

        //    // 2️⃣ Fetch all related expense rows using ExpenseID also
        //    var expenseItems = await _context.ExpenseLogBook
        //        .Where(e => e.EmpID == empId
        //                    && e.SubmissionDate == submissionDate
        //                    && e.ProjectCode == projectCode
        //                    && e.ExpenseID == expenseId)
        //        .ToListAsync();

        //    // 3️⃣ Fetch expense type list for dropdown
        //    var types = await _context.TypeOfExpense.ToListAsync();

        //    // 4️⃣ Pass additional info to view
        //    ViewBag.Employee = employee;
        //    ViewBag.Types = types;
        //    ViewBag.ProjectCode = projectCode;
        //    ViewBag.SubmissionDate = submissionDate.ToString("yyyy-MM-dd");
        //    ViewBag.ExpenseID = expenseId; // ✅ send ExpenseID to view

        //    // 5️⃣ Return view with list of expense items
        //    return View("GroupEdit", expenseItems);
        //}

        //[HttpPost("/api/dashboard/updategroup")]
        //public async Task<IActionResult> UpdateGroup([FromBody] List<ExpenseLogBook> items)
        //{
        //    foreach (var item in items)
        //    {
        //        if (item.ID > 0)
        //            _context.ExpenseLogBook.Update(item);
        //        else
        //            await _context.ExpenseLogBook.AddAsync(item);
        //    }
        //    await _context.SaveChangesAsync();
        //    return Ok();
        //}

        //public async Task<IActionResult> ViewBill(long id)
        //{
        //    var expense = await _context.ExpenseLogBook.FindAsync(id);
        //    if (expense == null || expense.BillDocument == null)
        //        return NotFound();

        //    return File(expense.BillDocument, expense.BillContentType ?? "application/octet-stream", expense.BillFileName);
        //}

        //[HttpGet("/api/expenseedit/GetBillInfo/{id}")]
        //public async Task<IActionResult> GetBillInfo(long id)
        //{
        //    var expense = await _context.ExpenseLogBook.FindAsync(id);
        //    if (expense == null || expense.BillDocument == null)
        //        return NotFound();

        //    var base64 = Convert.ToBase64String(expense.BillDocument);
        //    var fileUrl = $"data:{expense.BillContentType};base64,{base64}";

        //    return Ok(new
        //    {
        //        fileUrl,
        //        contentType = expense.BillContentType,
        //        fileName = expense.BillFileName
        //    });
        //}

        //public async Task<IActionResult> GroupEdit(long expenseID)
        //{
        //    var empId = User.FindFirst("EmpID")?.Value ?? "";
        //    var employee = await _context.EmployeeDetails.FirstOrDefaultAsync(e => e.EmpID == empId);
        //    var model = await _context.ExpenseLogBook
        //                    .Where(x => x.ExpenseID == expenseID)
        //                    .ToListAsync();

        //    ViewBag.Employee = employee;
        //    ViewBag.TypeList = await _context.TypeOfExpense.ToListAsync();
        //    ViewBag.EmployeeList = await _context.EmployeeDetails.ToListAsync();
        //    ViewBag.ProjectCode = model.FirstOrDefault()?.ProjectCode;
        //    ViewBag.SubmissionDate = model.FirstOrDefault()?.SubmissionDate;

        //    return View(model);
        //}

        //[HttpGet("api/expenselimit/getlimit")]
        //public async Task<IActionResult> GetExpenseLimit(string level, string typeOfExpense)
        //{
        //    var limit = await _context.ExpenseLimitDetails
        //        .FirstOrDefaultAsync(x => x.Level == level && x.TypeOfExpense == typeOfExpense);

        //    if (limit == null) return NotFound();

        //    return Ok(new
        //    {
        //        maxLimitWithBill = limit.MaxLimitWithBill,
        //        maxLimitWOBill = limit.MaxLimitWOBill
        //    });
        //}


        //[HttpPost]
        //public async Task<IActionResult> SaveExpenses(List<ExpenseLogBook> model, List<IFormFile> Bills, string EmpID, string IRB, string ProjectCode, string SiteName, DateTime SubmissionDate)
        //{
        //    for (int i = 0; i < model.Count; i++)
        //    {
        //        var exp = model[i];
        //        exp.EmpID = EmpID;
        //        exp.IRB = IRB;
        //        exp.ProjectCode = ProjectCode;
        //        exp.SiteName = SiteName;
        //        exp.SubmissionDate = SubmissionDate;
        //        exp.Status = "Pending";

        //        if (Bills.Count > i)
        //        {
        //            using var ms = new MemoryStream();
        //            await Bills[i].CopyToAsync(ms);
        //            exp.BillDocument = ms.ToArray();
        //            exp.BillFileName = Bills[i].FileName;
        //            exp.BillContentType = Bills[i].ContentType;
        //        }

        //        _context.ExpenseLogBook.Add(exp);
        //    }

        //    await _context.SaveChangesAsync();
        //    return Ok();
        //}


        //[HttpPost("update/{id}")]
        //public async Task<IActionResult> UpdateExpense(long id, [FromForm] ExpenseUpdateDto dto)
        //{
        //    var expense = await _context.ExpenseLogBook.FirstOrDefaultAsync(x => x.ID == id);
        //    if (expense == null)
        //        return NotFound("Expense not found.");

        //    expense.TypeOfExpense = dto.TypeOfExpense;
        //    expense.TravelLocation = dto.TravelLocation;
        //    expense.Quantity = dto.Quantity;
        //    expense.FellowMembers = dto.FellowMembers;
        //    expense.ClaimAmount = dto.ClaimAmount;
        //    expense.SanctionedAmount = dto.SanctionedAmount;
        //    expense.DateofExpense = dto.DateofExpense;
        //    expense.RequireSpecialApproval = dto.RequireSpecialApproval;
        //    expense.SubmissionDate = dto.SubmissionDate;
        //    expense.ExpenseID = dto.ExpenseID;

        //    if (dto.BillDocument != null && dto.BillDocument.Length > 0)
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            await dto.BillDocument.CopyToAsync(ms);
        //            expense.BillDocument = ms.ToArray();
        //            expense.BillFileName = dto.BillDocument.FileName;
        //            expense.BillContentType = dto.BillDocument.ContentType;
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //    return Ok();
        //}

        //delete the particular expense
        [HttpDelete("/api/expenseedit/delete/{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.ExpenseLogBook
     .FirstOrDefaultAsync(e => e.ID == id);


            if (expense == null)
                return NotFound();

            _context.ExpenseLogBook.Remove(expense);
            await _context.SaveChangesAsync();

            return Ok();
        }

    }
    public class ChangeDTO
    {
        public string? SiteName { get; set; }
        public string? ProjectCode { get; set; }
    }


    public class ExpenseUpdateDto
    {
        public long ID { get; set; }
        public string? TypeOfExpense { get; set; }
        public string? TravelLocation { get; set; }
        public int? Quantity { get; set; }
        public string? FellowMembers { get; set; }
        public double? ClaimAmount { get; set; }
        public double? SanctionedAmount { get; set; }
        public DateTime? DateofExpense { get; set; }
        public string? RequireSpecialApproval { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public long ExpenseID { get; set; }
        public IFormFile? BillDocument { get; set; }
    }
}


