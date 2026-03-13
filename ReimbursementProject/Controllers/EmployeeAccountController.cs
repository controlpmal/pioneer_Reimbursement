using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using System.Globalization;
using ReimbursementProject.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static System.Net.WebRequestMethods;

namespace ReimbursementProject.Controllers
{
    public class EmployeeAccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;

        public EmployeeAccountController(ApplicationDbContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
        }

        // GET: /EmployeeAccount/Create
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Details()
        {
            return View();
        }

        public IActionResult Index()
        {
            var location = User.FindFirst("CompanyLocation")?.Value;

            var emplist = _context.EmployeeDetails
                                  .Where(x => x.CompanyLocation == location)
                                  .Select(x => x.EmpID)
                                  .ToList();

            var impressList = _context.EmployeeImpress
                                      .Where(e => emplist.Contains(e.EmpID) && e.STATUS != "accept")
                                      .OrderByDescending(e => e.DATETIME)
                                      .ToList();

            var accountList = _context.EmployeeAccount.Where(a => emplist.Contains(a.EMP_ID))
                                      .OrderByDescending(a => a.DATETIME)
                                      .ToList();

            return View(Tuple.Create(impressList, accountList));
        }

        //accept impress
        [HttpPost]
        public IActionResult AcceptImpress(long id, double? advanceAmount, string? remark)
        {
            var designation = User.FindFirst("Designation")?.Value;
            var record = _context.EmployeeImpress.FirstOrDefault(e => e.ID == id);
            if (record == null) return Json(new { message = "Record not found" });


            // If Advance not entered → use Impress Amount
            //record.ADVANCE_AMOUNT = -advanceAmount ?? record.IMPRESS_AMOUNT;
            //record.REMARK = string.IsNullOrWhiteSpace(remark) ? null : remark;
            //record.STATUS = "accept";
            //record.DATETIME = DateTime.Now;

            if (designation == "HR"||designation=="Head")
            {
                record.ADVANCE_AMOUNT = -advanceAmount ?? record.IMPRESS_AMOUNT;
                record.REMARK = string.IsNullOrWhiteSpace(remark) ? null : remark;
                record.STATUS = "inProcess";
                record.DATETIME = DateTime.Now;
            }
            if (designation == "AGM"||designation=="Consultant")
            {
                record.ADVANCE_AMOUNT = advanceAmount ?? record.IMPRESS_AMOUNT;
                record.REMARK = string.IsNullOrWhiteSpace(remark) ? null : remark;
                record.STATUS = "accept";
                record.DATETIME = DateTime.Now;
                var acc = new EmployeeAccount
                {
                    EMP_ID = record.EmpID,
                    EMP_NAME = record.EmpName,

                    IMPRESS_AMOUNT = record.ADVANCE_AMOUNT,
                    DATETIME = DateTime.Now,
                    PAYMENT_TYPE = "IMPRESS",
                    PROJECT_CODE = record.PROJECT_DETAILS,
                    MODE = record.MODE


                };
                _context.EmployeeAccount.Add(acc);
            }
            // Move to EmployeeAccount


            _context.SaveChanges();

            return Json(new { message = "Accepted Successfully!" });
        }

        [HttpPost]
        public IActionResult RejectImpress(long id, string? remark)
        {
            var record = _context.EmployeeImpress.FirstOrDefault(e => e.ID == id);
            if (record == null) return Json(new { message = "Record not found" });

            record.STATUS = "reject";
            record.REMARK = string.IsNullOrWhiteSpace(remark) ? null : remark;
            record.DATETIME = DateTime.Now;

            _context.SaveChanges();

            return Json(new { message = "Rejected Successfully!" });
        }




        // ✅ Get pending impress list (IMPRESS_AMOUNT != null)
        [HttpGet]
        public IActionResult GetPendingImpress()
        {
            var location = User.FindFirst("CompanyLocation")?.Value;

            var emplist = _context.EmployeeDetails
                                  .Where(x => x.CompanyLocation == location)
                                  .Select(x => x.EmpID)
                                  .ToList();

            var list = _context.EmployeeAccount
                .Where(x => x.IMPRESS_AMOUNT != null && emplist.Contains(x.EMP_ID))
                .Select(x => new {
                    x.ID,
                    x.EMP_ID,
                    x.EMP_NAME,
                    x.IMPRESS_AMOUNT,
                    x.PROJECT_CODE,
                    x.PAYMENT_TYPE,
                    x.DATETIME,
                    x.MODE
                }).ToList();

            return Json(list);
        }

        // ✅ Filter list where IMPRESS_AMOUNT == null
        [HttpGet]
        public IActionResult GetFilteredList(DateTime? fromDate, DateTime? toDate, string? searchText)
        {
            var query = _context.EmployeeAccount.AsQueryable();

            query = query.Where(x => x.IMPRESS_AMOUNT == null);

            if (fromDate.HasValue)
                query = query.Where(x => x.DATETIME >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.DATETIME <= toDate.Value);

            if (!string.IsNullOrEmpty(searchText))
                query = query.Where(x => x.EMP_ID.Contains(searchText) || x.EMP_NAME.Contains(searchText));

            var list = query.Select(x => new {
                x.EMP_ID,
                x.EMP_NAME,
                x.ADVANCE_AMOUNT,
                x.PAYMENT_TYPE,
                x.DATETIME
            }).ToList();

            return Json(list);
        }

        // ✅ Accept Impress -> move amount & update
        [HttpPost]
        public IActionResult AcceptImpressAccounts(int id, string approvalEmp)
        {
            var record = _context.EmployeeAccount.FirstOrDefault(x => x.ID == id);
            if (record == null) return Json(new { message = "Record not found!" });

            record.ADVANCE_AMOUNT = (record.ADVANCE_AMOUNT ?? 0) + (record.IMPRESS_AMOUNT ?? 0);
            record.IMPRESS_AMOUNT = null;
            record.APPROVAL_EMP = approvalEmp;
            record.DATETIME = DateTime.Now;

            _context.SaveChanges();

            return Json(new { message = "Impress amount approved successfully!" });
        }


        //[HttpGet]
        //public IActionResult getToatalamount( string empid)
        //{
        //    var data = _context.EmployeeAccount.Where(e=>e.EMP_ID==empid)
        //        .GroupBy(e => e.EMP_ID)
        //        .Select(g => new
        //        {
        //            EMP_ID = g.Key,
        //            EMP_NAME = g.First().EMP_NAME, // or g.Max(e => e.EMP_NAME)
        //            TotalAdvance = g.Sum(x => x.ADVANCE_AMOUNT ?? 0),
        //            LastTransaction = g.Max(x => x.DATETIME)
        //        })
        //        .ToList();

        //    return View(data);
        //}

        //[HttpGet]

        //public IActionResult getEmployeeName(string empid)
        //{
        //    var data = _context.EmployeeDetails.Where(e => e.EmpID == empid)

        //        .Select(g=> g.EmpName)
        //        .ToList();

        //    return View(data);
        //}



        // GET: /EmployeeAccount/Impress
        public IActionResult Impress()
        {
            var empId = User.FindFirst("EmpID")?.Value ?? "";

            // Pending list for only this employee
            var pendingList = _context.EmployeeImpress
                .Where(e => e.EmpID == empId && e.IMPRESS_AMOUNT != null&&e.STATUS!="accept")
                .OrderByDescending(e => e.DATETIME)
                .ToList();
            var rejectList = _context.EmployeeImpress.Where(e => e.EmpID == empId && e.STATUS == "reject").OrderByDescending(e => e.DATETIME).ToList();
            // History list for only this employee
            var historyList = _context.EmployeeAccount
                .Where(e => e.EMP_ID == empId && e.IMPRESS_AMOUNT == null)
                .OrderByDescending(e => e.DATETIME)
                .ToList();

            // Total advance amount
            double totalAdvance = historyList.Sum(e => e.ADVANCE_AMOUNT ?? 0);
            ViewBag.RejectCount = rejectList.Count;
            ViewBag.RejectList = rejectList;
            ViewBag.PendingCount = pendingList.Count;
            ViewBag.PendingList = pendingList;
            ViewBag.HistoryList = historyList;
            ViewBag.TotalAdvance = totalAdvance;

            return View();
        }
        // POST: /EmployeeAccount/AddImpress
        [HttpPost]
        public IActionResult AddImpress(double impressAmount ,string projectdetails,string Mode)
        {
            var empId = User.FindFirst("EmpID")?.Value ?? "";
            var empName = User.FindFirst("EmpName")?.Value ?? "";

            var newRecord = new EmployeeImpress
            {
                EmpID = empId,
                EmpName = empName,
                IMPRESS_AMOUNT = -impressAmount,
                PROJECT_DETAILS=projectdetails,
                DATETIME = DateTime.Now,
                STATUS="pending",
                MODE=Mode
            };

            var toEmail = "kuldeep@pmalgroup.com";// ⬅️ Add this line


            // Create email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("PMAL Control", _emailSettings.Email));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = "IMRESS REQUEST";
            emailMessage.Body = new TextPart("plain")
            {
                Text = $"Hello,\n\n Impress request  {empId} name {empName} amount {impressAmount} for project {projectdetails}\n\nRegards,\nPMAL Control"
            };

            // Send email
            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(_emailSettings.Email, _emailSettings.Password);
                client.Send(emailMessage);
                client.Disconnect(true);
            }


            _context.EmployeeImpress.Add(newRecord);
            _context.SaveChanges();

            return RedirectToAction("Impress");
        }

        // POST: /EmployeeAccount/DeleteImpress
        [HttpPost]
        public IActionResult DeleteImpress(int id)
        {
            var record = _context.EmployeeImpress.FirstOrDefault(e => e.ID == id);
            if (record != null)
            {
                _context.EmployeeImpress.Remove(record);
                _context.SaveChanges();
            }

            return RedirectToAction("Impress");
        }


        [HttpGet]
        public IActionResult Create(string empid)
        {
            var model = new EmployeeAccount();

            if (!string.IsNullOrEmpty(empid))
            {
                // Fetch employee name
                var emp = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empid);
                if (emp != null)
                {
                    model.EMP_ID = emp.EmpID;
                    model.EMP_NAME = emp.EmpName;
                }

                // Fetch total advance from EmployeeAccount
                var totalData = _context.EmployeeAccount
                    .Where(e => e.EMP_ID == empid)
                    .GroupBy(e => e.EMP_ID)
                    .Select(g => new
                    {
                        TotalAdvance = g.Sum(x => x.ADVANCE_AMOUNT ?? 0)
                    })
                    .FirstOrDefault();

                if (totalData != null)
                {
                    model.ADVANCE_AMOUNT = totalData.TotalAdvance;
                }
            }

            model.DATETIME = DateTime.Now;

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(EmployeeAccount model)
        {
            if (ModelState.IsValid)
            {
                // Negative for IMPRESS or BILL APPROVAL
                if (model.PAYMENT_TYPE == "IMPRESS" || model.PAYMENT_TYPE == "BILL APPROVAL")
                {
                    if (model.PAYMENT_TYPE == "BILL APPROVAL")
                    {
                        model.PAYMENT_TYPE = "BILL BALANCE PAYMENT";
                    }
                    model.ADVANCE_AMOUNT = -(Math.Abs(model.ADVANCE_AMOUNT ?? 0));
                }
                else if (model.PAYMENT_TYPE == "BILL"||model.PAYMENT_TYPE== "SALARY")
                {
                    if(model.PAYMENT_TYPE== "BILL")
                    {
                        model.PAYMENT_TYPE = "BILL APPROVED";
                    }
                   
                    model.ADVANCE_AMOUNT = Math.Abs(model.ADVANCE_AMOUNT ?? 0);
                }

                model.DATETIME = DateTime.Now;

                _context.EmployeeAccount.Add(model);
                _context.SaveChanges();

                return RedirectToAction("Create", new { empid = model.EMP_ID });
            }
            return View(model);
        }
    }
}
