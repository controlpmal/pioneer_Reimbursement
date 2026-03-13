using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using MimeKit;
using Org.BouncyCastle.Ocsp;
using ReimbursementProject.Data;
using ReimbursementProject.Hubs;
using ReimbursementProject.Models;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace ReimbursementProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

  
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly StoreAppDbContext _storecontext;
        private readonly EmailSettings _emailSettings;
        public EmployeeController(
        ApplicationDbContext context,
        StoreAppDbContext storecontext,
        IOptions<EmailSettings> emailSettings,
        IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _storecontext = storecontext;
            _emailSettings = emailSettings.Value;
            _hubContext = hubContext;
        }

        private readonly IHubContext<NotificationHub> _hubContext;

       
           
        

        // ✅ GET: /api/MasterData/employees
        [HttpGet("employees1")]
        public IActionResult GetEmployees1()
        {
            var employees = _context.EmployeeDetails.ToList();
         
            return Ok(employees);
        }



        [HttpPost("DoLogin")]
        public async Task<IActionResult> DoLogin([FromBody] LoginDto dto)
        {
            if (dto == null) return BadRequest(new { message = "Invalid request" });

            // Look up user server-side (do NOT send all users to the client)
            var employee = _context.EmployeeDetails.SingleOrDefault(e => e.EmpID == dto.EmpId);
            if (employee == null) return BadRequest(new { message = "Employee ID not found." });

            // TODO: Replace plain-text compare with hashed password compare
            if (employee.Password != dto.Password) return BadRequest(new { message = "Password is incorrect." });

            if (string.IsNullOrWhiteSpace(employee.Status)) return BadRequest(new { message = "Pending approval from HR." });
            if (employee.Status.ToUpper() != "OK") return BadRequest(new { message = "Not approved from HR." });

            // Create claims (only store non-sensitive info)
            var claims = new List<Claim>
{
    new Claim("EmpID", employee.EmpID),
    new Claim("EmpName", employee.EmpName ?? string.Empty),
    new Claim("Level", employee.Level ?? string.Empty),
    new Claim("Designation", employee.Designation ?? string.Empty),
    new Claim("IRB", employee.IRB ?? string.Empty),
     new Claim("CompanyLocation",employee.CompanyLocation??string.Empty)
};

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });
            
            // Optionally also set small server session values (not required):
            HttpContext.Session.SetString("EmpID", employee.EmpID);

            // 🔔 Send notification to all connected browsers
            await _hubContext.Clients.All.SendAsync("ReceiveNotification",
                $"👋 {employee.EmpName} has logged in successfully!");
            _context.Notifications.Add(new Notification
            {
                EmpID = employee.EmpID, // or based on designation
                Title = "New Login",
                Message = "New user login",
                Type = "Login"
            });
            await _context.SaveChangesAsync();

            return Ok(new { success = true, redirectUrl = Url.Action("Dashboard", "Home") });
        }

        [HttpGet("test-notify")]
        public async Task<IActionResult> TestNotify()
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", "🔔 Test notification from server!");
            return Ok("Notification sent!");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Ok(new { success = true });
        }


        [HttpGet("employees")]
        public IActionResult GetEmployees()
        {
            var employees = _context.EmployeeDetails
                .Select(e => new { EmpID = e.EmpID, EmpName = e.EmpName, Designation = e.Designation, Level = e.Level })
                .ToList();
            return Ok(employees);
        }



        [HttpGet("totalAmount")]
        public IActionResult getToatalamount(string empid)
        {
            var data = _context.EmployeeAccount.Where(e => e.EMP_ID == empid)
                .GroupBy(e => e.EMP_ID)
                .Select(g => new
                {
                    EMP_ID = g.Key,
                    EMP_NAME = g.First().EMP_NAME, // or g.Max(e => e.EMP_NAME)
                    TotalAdvance = g.Sum(x => x.ADVANCE_AMOUNT ?? 0),
                    LastTransaction = g.Max(x => x.DATETIME)
                })
                .ToList();

            return Ok(data);
        }

        [HttpGet("employeeName")]

        public IActionResult getEmployeeName(string empid)
        {
            var data = _context.EmployeeDetails.Where(e => e.EmpID == empid)

                .Select(g => g.EmpName)
                .ToList();

            return Ok(data);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string empid)
        {
            if (string.IsNullOrEmpty(empid))
                return BadRequest("empid is required");

            var history = await _context.EmployeeAccount
                .Where(e => e.EMP_ID == empid)
                .OrderByDescending(e => e.DATETIME)
                .ToListAsync();

            if (history == null || history.Count == 0)
                return NotFound("No history found for this empid");

            return Ok(history);
        }


        // ✅ GET: /api/MasterData/expenseTypes
        //[HttpGet("expenseTypes")]
        //public IActionResult GetExpenseTypes()
        //{
        //    var expenses = _context.TypeOfExpense.ToList();
        //    return Ok(expenses);
        //}
        [HttpGet("expenseTypes")]
        public IActionResult GetExpenseTypes()
        {
            var expenses = _context.TypeOfExpense
                .Select(t => new { t.ID, TypeOfExpense = t.TypeOfExpense })
                .ToList();
            return Ok(expenses);
        }




        //// ✅ GET: /api/MasterData/expenseLimits
        //[HttpGet("expenseLimits")]
        //public IActionResult GetExpenseLimits()
        //{
        //    var limits = _context.ExpenseLimitDetails.ToList();
        //    return Ok(limits);
        //}


        [HttpGet("expenseLimits")]
        public IActionResult GetExpenseLimits(string level, string type, bool withBill = true)
        {
            // find the matching limit row (Level + TypeOfExpense)
            var row = _context.ExpenseLimitDetails
                        .Where(x => x.Level == level && x.TypeOfExpense == type)
                        .FirstOrDefault();

            double maxLimit = 0;
            if (row != null)
            {
                maxLimit = withBill ? (row.MaxLimitWithBill ?? 0) : (row.MaxLimitWOBill ?? 0);
            }

            return Ok(new { maxLimit });
        }




        //[HttpPost("save")]
        //public IActionResult SaveExpenses([FromBody] List<ExpenseLogBook> expenses)
        //{
        //    if (expenses == null || !expenses.Any())
        //        return BadRequest("No expenses provided.");
        //    var lastId = _context.ExpenseLogBook
        //             .OrderByDescending(e => e.ID)
        //             .Select(e => e.ID)
        //             .FirstOrDefault();


        //    foreach (var exp in expenses)
        //    {
        //        exp.SubmissionDate = DateTime.Now;  // current datetime
        //        exp.Status = "0";
        //        exp.Quantity = exp.Quantity == 0 ? 1 : exp.Quantity;

        //        // default pending
        //    }

        //    _context.ExpenseLogBook.AddRange(expenses);
        //    _context.SaveChanges();

        //    return Ok(new { message = "Expenses saved successfully!" });
        //}
        //[HttpGet("pendingCount")]
        //public IActionResult GetPendingCount()
        //{
        //    var count = _context.ExpenseLogBook
        //        .Where(e => e.Status == "0")
        //        .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode })
        //        .Count();

        //    return Ok(count);
        //}

        // ✅ Register API
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] EmployeeDetails emp)
        {
            try
            {
                if (await _context.EmployeeDetails.AnyAsync(e => e.EmpID == emp.EmpID))
                    return BadRequest(new { message = "EmpID already exists" });

                if (await _context.EmployeeDetails.AnyAsync(e => e.MailID == emp.MailID))
                    return BadRequest(new { message = "Email already registered" });

                /*emp.Status = null;*/ // Default: waiting for HR approval

                _context.EmployeeDetails.Add(emp);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Registration successful. Waiting for HR approval" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error saving data", error = ex.InnerException?.Message ?? ex.Message });
            }
        }




        [HttpGet("send")]
        public IActionResult SendOtp(string toEmail)
        {
            try
            {
                // Generate OTP
                Random random = new Random();
                string otp = random.Next(1000, 9999).ToString();

                // Create email
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("PMAL Control", _emailSettings.Email));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = "Your OTP Code";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Hello,\n\nYour OTP is: {otp}\n\nRegards,\nPMAL Control"
                };

                // Send email
                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(_emailSettings.Email, _emailSettings.Password);
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }

                return Ok(new { Success = true, OTP = otp, Message = "OTP sent successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }


        [HttpPost("newName")]
        public IActionResult ChangePassword(string newName)
        {

            var empId = User.FindFirst("EmpID")?.Value;
            var storeemployee = _storecontext.Employees.FirstOrDefault(e => e.UserName == empId);

            var employee = _context.EmployeeDetails
                .FirstOrDefault(e => e.EmpID == empId);

            if (employee == null)
                return NotFound();


            // 🔐 Ideally hash the password
            employee.EmpName = newName;
            if (storeemployee == null)
            {

            }
            else
            {
                storeemployee.FullName = newName;
                _storecontext.SaveChanges();
            }



            _context.SaveChanges();



            return Ok(new { message = "name change succeffuly" });
        }








        public class ExpenseRowDto
        {
            public string DateOfExpense { get; set; }
            public string TypeOfExpense { get; set; }
            public string? TravelLocation { get; set; }
            public double? KM { get; set; }
            public int? Quantity { get; set; }
            public string? FellowMembers { get; set; }
            public string BillType { get; set; } // "with" or "without"
            public double? SanctionedAmount { get; set; }
            public double? ClaimAmount { get; set; }
            public int? FileIndex { get; set; } // maps to file_i in form data
        }

        [HttpPost("submit")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        public async Task<IActionResult> Submit()
        {
            var form = Request.Form;
            var empId = form["EmpID"].ToString();
            var empName = User.FindFirst("EmpName")?.Value;
            var site = form["SiteName"].ToString();
            var project = form["ProjectCode"].ToString();
            var level = form["Level"].ToString();
            var irb = form["IRB"].ToString();
            var rowsJson = form["Rows"].ToString();

            if (string.IsNullOrWhiteSpace(empId) || string.IsNullOrWhiteSpace(rowsJson))
                return BadRequest(new { success = false, message = "Missing data" });

            List<ExpenseRowDto> rows;
            try
            {
                rows = JsonSerializer.Deserialize<List<ExpenseRowDto>>(rowsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Invalid rows JSON" });
            }

            // Create master bill
            var now = DateTime.Now;
            var expenseBill = new ExpenseBillBook
            {
                ExpenseBillNumber = $"EBL{empId}{now:yyyyMMddHHmmss}",
                SubmissionDate = now,
                Status = "Pending"
            };

            _context.ExpenseBillBook.Add(expenseBill);
            await _context.SaveChangesAsync();

            // Process each row
            foreach (var r in rows)
            {
                // Parse date
                DateTime? expenseDate = null;
                if (!string.IsNullOrWhiteSpace(r.DateOfExpense))
                {
                    string[] formats = { "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" };
                    if (DateTime.TryParseExact(r.DateOfExpense, formats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        expenseDate = parsed;
                    }
                    else if (DateTime.TryParse(r.DateOfExpense, out var fallback))
                    {
                        expenseDate = fallback;
                    }
                }

                var el = new ExpenseLogBook
                {
                    SiteName = site,
                    ProjectCode = project,
                    EmpID = empId,
                    SubmissionDate = now,
                    DateofExpense = expenseDate,
                    TypeOfExpense = r.TypeOfExpense,
                    Quantity = r.Quantity,
                    TravelLocation = r.TravelLocation,
                    FellowMembers = r.FellowMembers,
                    ClaimAmount = r.ClaimAmount,
                    IRB = irb,
                    Status = "5",
                    //RequireSpecialApproval = (r.ClaimAmount > (r.SanctionedAmount ?? 0)) ? "true" : "false",
                    Rejection = null,
                    ExpenseID = expenseBill.ExpenseID,
                    //SanctionedAmount = (r.ClaimAmount > (r.SanctionedAmount ?? 0))
                    //                    ? r.SanctionedAmount
                    //                    : r.ClaimAmount
                };

                // File Upload
                if (r.FileIndex != null)
                {
                    var fileKey = $"file_{r.FileIndex}";
                    var file = Request.Form.Files.FirstOrDefault(f => f.Name == fileKey);

                    if (file != null && file.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        byte[] fileBytes = ms.ToArray();

                        if (fileBytes.Length > 1 * 1024 * 1024)
                            fileBytes = CompressFile(fileBytes, file.FileName);

                        el.BillDocument = fileBytes;
                        el.BillFileName = file.FileName ?? "captured_file";
                        el.BillContentType = file.ContentType ?? "application/octet-stream";
                    }
                }
                // ✅ Compute sanctioned amount
                double computedSanctioned = 0;
                var limitRow = _context.ExpenseLimitDetails
                                       .FirstOrDefault(x => x.Level == level && x.TypeOfExpense == r.TypeOfExpense);
                double maxLimit = 0;
                if (limitRow != null)
                {
                    maxLimit = (r.BillType?.ToLower() == "with")
                        ? (limitRow.MaxLimitWithBill ?? 0)
                        : (limitRow.MaxLimitWOBill ?? 0);
                }

                bool isTravel = (r.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL BIKE")
                                || (r.TypeOfExpense ?? "").ToUpper().Contains("TRAVEL CAR");
                if (isTravel) computedSanctioned = (r.KM ?? 0) * maxLimit;
                else computedSanctioned = (r.Quantity ?? 0) * maxLimit;
                int computedSanctionedInt = (int)Math.Floor(computedSanctioned);
                if (computedSanctionedInt == null)
                {
                    computedSanctionedInt = 0;
                }
                el.SanctionedAmount = (r.ClaimAmount > (computedSanctionedInt))
                                        ? computedSanctionedInt
                                        : r.ClaimAmount;
                el.RequireSpecialApproval = (r.ClaimAmount > (computedSanctionedInt)) ? "true" : "false";



                _context.ExpenseLogBook.Add(el);
            }

            _context.Notifications.Add(new Notification
            {
                EmpID = empId,
                Title = "New bill",
                Message = "New bill filled ",
                Type = "reimbursement bill"
            });
            await _context.SaveChangesAsync();

          

            return Ok(new { success = true, billNo = expenseBill.ExpenseBillNumber });
        }



        private byte[] CompressFile(byte[] fileBytes, string fileName)
        {
            try
            {
                // Get file extension
                string ext = Path.GetExtension(fileName)?.ToLower() ?? "";

                // ✅ Handle images (JPG, PNG, JPEG)
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    using var input = new MemoryStream(fileBytes);
                    using var output = new MemoryStream();
                    using (var image = System.Drawing.Image.FromStream(input))
                    {
                        var encoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
                        var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                        encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L); // 60% quality
                        image.Save(output, encoder, encoderParams);
                    }
                    return output.ToArray();
                }

                //// ✅ Handle PDF / Excel — compress using GZip
                //if (ext == ".pdf" || ext == ".xls" || ext == ".xlsx")
                //{
                //    using var input = new MemoryStream(fileBytes);
                //    using var output = new MemoryStream();
                //    using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
                //    {
                //        input.CopyTo(gzip);
                //    }
                //    return output.ToArray();
                //}

                // Default — no compression
                return fileBytes;
            }
            catch
            {
                // In case compression fails, return original file
                return fileBytes;
            }
        }

        private static System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            return System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders()
                .FirstOrDefault(c => c.FormatID == format.Guid);
        }



        [HttpGet("new")]
        public IActionResult GetNewEmployees()
        {
            var employees = _context.EmployeeDetails
                .Where(e => e.Status != "OK")
                .Select(e => new {
                    empId = e.EmpID,
                    empName = e.EmpName,
                    empLevel=e.Level,
                    empDesignation=e.Designation,
                    empIrb=e.IRB,
                    empDepartment=e.Dept,
                    empMail = e.MailID,
                    empLocation=e.CompanyLocation,  // ⬅️ Add this line
                    photoUrl = "/uploads/employees/" + e.EmpID + ".jpg" // store photos with EmpID.jpg
                })
                .ToList();

            return Ok(employees);
        }

        // Approve employee
        [HttpPost("approve/{empId}")]
        public IActionResult ApproveEmployee(string empId, [FromBody] EmployeeDetails updatedEmp)
        {
            var emp = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empId);
            if (emp == null) return NotFound(new { success = false, message = "Employee not found" });

            // Update editable fields
            emp.IRB = updatedEmp.IRB;
            emp.Level = updatedEmp.Level;
            emp.Designation = updatedEmp.Designation;
            emp.Dept = updatedEmp.Dept;
            emp.CompanyLocation= updatedEmp.CompanyLocation;
            
            var toEmail = emp.MailID;// ⬅️ Add this line

            // Create email
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("PMAL Control", _emailSettings.Email));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = "Reimbursement Approvel";
            emailMessage.Body = new TextPart("plain")
            {
                Text = $"Hello,\n\n Approved from HR to access the reimbursment website\n\nRegards,\nPMAL Control"
            };

            // Send email
            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                client.Authenticate(_emailSettings.Email, _emailSettings.Password);
                client.Send(emailMessage);
                client.Disconnect(true);
            }

            // Approve employee
            emp.Status = "OK";
            // 🔹 6. Add record in Store DB
            var storeEmp = new StoreEmployeeDetails
            {
                UserName = emp.EmpID,               // Username = EmpID
                Password = emp.Password,            // Assuming `Password` exists in your EmployeeDetails model
                FullName = emp.EmpName,             // Full name from main table
                Department = emp.Dept,
                IRB = emp.IRB,// Department
                Role = "Employee" ,
                Level=emp.Level// Default role
            };
           

          

            _storecontext.Employees.Add(storeEmp);
            _storecontext.SaveChanges();
            _context.SaveChanges();
            return Ok(new { success = true, message = "Employee approved and updated successfully" });
        }


        // Get all employees
        [HttpGet("all")]
        public IActionResult GetAllEmployees()
        {
            return Ok(_context.EmployeeDetails.ToList());
        }

        // Delete employee
        // Delete employee
        [HttpDelete("{empId}")]
        public IActionResult DeleteEmployee(string empId)
        {
            var emp = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empId);
            if (emp == null) return NotFound();

            var employee = emp.EmpID;
            var employeename = emp.EmpName;
            var toEmail = emp.MailID;
            // Create email
            if (!toEmail.IsNullOrEmpty())
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("PMAL Control", _emailSettings.Email));
                emailMessage.To.Add(new MailboxAddress("", toEmail));
                emailMessage.Subject = "Reject & Delete ";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Hello,\n\n Rejected from HR department whose empid is {employee} and name is {employeename}\n\nRegards,\nPMAL Control"
                };

                // Send email
                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(_emailSettings.Email, _emailSettings.Password);
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }
            }
           



            _context.EmployeeDetails.Remove(emp);
            _context.SaveChanges();

            return Ok(new { success = true });
        }

        // Edit employee
        [HttpPut("{empId}")]
        public IActionResult EditEmployee(string empId, [FromBody] EmployeeDetails updated)
        {
            var emp = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empId);
            if (emp == null) return NotFound();

            emp.EmpName = updated.EmpName;
            emp.Designation = updated.Designation;
            emp.Level = updated.Level;
            emp.IRB = updated.IRB;
            emp.Dept = updated.Dept;
            emp.AdvanceAmount = updated.AdvanceAmount;
            emp.Password = updated.Password;
            emp.MailID = updated.MailID;
            emp.CompanyLocation = updated.CompanyLocation;

            _context.SaveChanges();

            return Ok(new { success = true });
        }


        [HttpGet("check-duplicate")]
        public IActionResult CheckDuplicate(string empId, string typeOfExpense, DateTime dateOfExpense)
        {
            if (string.IsNullOrEmpty(empId) || string.IsNullOrEmpty(typeOfExpense))
                return BadRequest(new { success = false, message = "Invalid input" });

            


            var duplicate = _context.ExpenseLogBook
                .Where(e => e.DateofExpense.HasValue
                            && e.DateofExpense.Value.Date == dateOfExpense.Date
                            && e.TypeOfExpense == typeOfExpense
                            && (
                                e.EmpID == empId ||
                                (e.FellowMembers != null && e.FellowMembers.Contains(empId))
                               )
                     )
                .FirstOrDefault();

            if (duplicate != null)
                return Ok(new { success = true, exists = true, message = "Duplicate expense found for this date and type." });

            return Ok(new { success = true, exists = false });
        }



        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] string empId)
        {
            var employee = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empId);
            if (employee == null || string.IsNullOrWhiteSpace(employee.MailID))
                return NotFound(new { success = false, message = "Employee not found or email not available." });

            // Reuse your SendOtp logic
            Random random = new Random();
            string otp = random.Next(1000, 9999).ToString();

            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("PMAL Control", "pmalcontrol@gmail.com"));
                emailMessage.To.Add(new MailboxAddress("", employee.MailID));
                emailMessage.Subject = "Your OTP Code";
                emailMessage.Body = new TextPart("plain")
                {
                    Text = $"Hello {employee.EmpName},\n\nYour OTP is: {otp}\n\nRegards,\nPMAL Control"
                };

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(_emailSettings.Email, _emailSettings.Password);
                    client.Send(emailMessage);
                    client.Disconnect(true);
                }

                // Store OTP in TempData / Memory / Cache / or return it for now (not safe for production)
                HttpContext.Session.SetString("ResetOtp", otp);
                HttpContext.Session.SetString("ResetEmpId", empId);

                return Ok(new { success = true, message = "OTP sent to your registered email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] string enteredOtp)
        {
            var storedOtp = HttpContext.Session.GetString("ResetOtp");
            if (storedOtp == enteredOtp)
            {
                return Ok(new { success = true, message = "OTP verified" });
            }

            return BadRequest(new { success = false, message = "Invalid OTP" });
        }


        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] PasswordResetModel model)
        {
            try
            {
                var empId = HttpContext.Session.GetString("ResetEmpId");
                if (string.IsNullOrEmpty(empId)) return Unauthorized();

                var emp = _context.EmployeeDetails.FirstOrDefault(e => e.EmpID == empId);
                if (emp == null) return NotFound(new { success = false, message = "Employee not found" });
                var storeEmp = _storecontext.Employees.FirstOrDefault(e => e.UserName == empId);
                emp.Password = model.NewPassword;
                storeEmp.Password = model.NewPassword;
                _storecontext.SaveChanges();
                _context.SaveChanges();

                HttpContext.Session.Remove("ResetEmpId");
                HttpContext.Session.Remove("ResetOtp");

                return Ok(new { success = true, message = "Password updated successfully" });
            }
            catch
            {
                return BadRequest( "Some data were missing ");
            }
          
        }

        public class PasswordResetModel
        {
            public string NewPassword { get; set; }
        }



        [HttpGet("pendingEmployeeAmount")]
        public IActionResult GetEmployeePendingAmount()
        {
            var data = _context.ExpenseLogBook
                .AsEnumerable() // switch to in-memory
                .Where(x =>
                    !string.IsNullOrEmpty(x.Status) &&
                    x.Rejection != "reject" &&
                    int.TryParse(x.Status, out int statusValue) &&
                    statusValue > 0 &&
                    statusValue < 4
                )
                .Join(_context.EmployeeDetails,
                      expense => expense.EmpID,
                      emp => emp.EmpID,
                      (expense, emp) => new { expense, emp })
                .GroupBy(x => new { x.expense.EmpID, x.emp.EmpName })
                .Select(g => new EmployeePendingAmountDto
                {
                    EmpID = g.Key.EmpID,
                    EmpName = g.Key.EmpName,
                    TotalClaimAmount = g.Sum(x => x.expense.ClaimAmount ?? 0)
                })
                .ToList();

            return Ok(data);
        }


        public class EmployeePendingAmountDto
        {
            public string EmpID { get; set; }
            public string EmpName { get; set; }   // 👈 Add this
            public double TotalClaimAmount { get; set; }
        }

        [HttpGet("projectAmount")]
        public IActionResult ProjectAmount()
        {
            var regex = new Regex(@"\b\d{5}\b"); // Match exactly 5 digit integer

            var data = _context.ExpenseLogBook
                .AsEnumerable()
                .Where(x => x.Status == "4")
                .Select(x =>
                {
                    string code = null;

                    // Check ProjectCode first
                    if (!string.IsNullOrEmpty(x.ProjectCode))
                    {
                        var match = regex.Match(x.ProjectCode);
                        if (match.Success)
                            code = match.Value;
                    }

                    // If not found, check TravelLocation
                    if (code == null && !string.IsNullOrEmpty(x.TravelLocation))
                    {
                        var match = regex.Match(x.TravelLocation);
                        if (match.Success)
                            code = match.Value;
                    }

                    // If still null → assign OTHER
                    if (string.IsNullOrEmpty(code))
                        code = "OTHER";

                    return new
                    {
                        Code = code,
                        SanctionedAmount = x.SanctionedAmount ?? 0
                    };
                })
                .GroupBy(x => x.Code)
                .Select(g => new ProjectSanctionedAmountDto
                {
                    Code = g.Key,
                    TotalSanctionedAmount = g.Sum(x => x.SanctionedAmount)
                })
                .OrderByDescending(x => x.TotalSanctionedAmount)
                .ToList();

            return Ok(data);
        }

        public class ProjectSanctionedAmountDto
        {
            public string Code { get; set; }
            public double TotalSanctionedAmount { get; set; }
        }


    }
}
