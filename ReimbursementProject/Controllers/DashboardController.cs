using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using ReimbursementProject.Data;
using ReimbursementProject.Models;
using System.Linq;
using System.Security.Policy;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using MailKit.Net.Smtp;

namespace ReimbursementProject.Controllers
{
    //[Authorize(AuthenticationSchemes = "EmployeeAuth")]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;

        public DashboardController(ApplicationDbContext context, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
        }

        //checked count get from here
        //  pending count get from here 
        [HttpGet("checked")]
        public IActionResult GetCheckedCount(string empid, string designation)
        {
            var employee = _context.ExpenseLogBook.FirstOrDefault(e => e.IRB == empid);
            IQueryable<ExpenseLogBook> query = _context.ExpenseLogBook.Where(e => e.Rejection != "reject");
            if (empid != null && empid != "")
            {








                query = query.Where(e => e.EmpID == empid && e.Status == "5");


                var count = query
                    .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode, e.ExpenseID })
                    .Count();

                return Ok(count);
            }
            else { return Ok(0); }
            ;
        }


        //  pending count get from here 
        [HttpGet("pending")]
        public IActionResult GetPendingCount(string empid, string designation)
        {
            var empLocation = _context.EmployeeDetails
                       .Where(x => x.EmpID == empid)
                       .Select(x => x.CompanyLocation)
                       .FirstOrDefault();
            List<string> sameLocationEmpIds;

            if (empLocation == null)
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == null)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            else
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == empLocation)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            var employee = _context.ExpenseLogBook.FirstOrDefault(e => e.IRB == empid);
            IQueryable<ExpenseLogBook> query = _context.ExpenseLogBook.Where(e => e.Rejection != "reject" && e.Status!="5");
            if (empid != null && empid != "")
            {




               
                if (designation == "HR")
                {
                    query = query.Where(e => (e.Status == "1") || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                }
                else if (designation == "AGM")
                {
                    query = query.Where(e => (e.Status == "2") || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                }
                else if (designation == "ACCOUNTS")
                {
                    query = query.Where(e => (e.Status == "3" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                }
                else if (employee != null)
                {
                    query = query.Where(e => (e.IRB == empid && e.Status == "0") || (e.EmpID == empid && e.Status != "4"));
                }
                else
                {
                    query = query.Where(e => e.EmpID == empid && e.Status != "4");
                }

                var count = query
                    .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode, e.ExpenseID })
                    .Count();

                return Ok(count);
            }
            else { return Ok(0); }
            ;
        }

        // approved count get from here 
        [HttpGet("approved")]
        public IActionResult GetApprovedCount(string empid, string designation)
        {
            var empLocation = _context.EmployeeDetails
                       .Where(x => x.EmpID == empid)
                       .Select(x => x.CompanyLocation)
                       .FirstOrDefault();
            List<string> sameLocationEmpIds;

            if (empLocation == null)
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == null)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            else
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == empLocation)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            var employee = _context.ExpenseLogBook.FirstOrDefault(e => e.IRB == empid);
            IQueryable<ExpenseLogBook> query = _context.ExpenseLogBook.Where(e => e.Rejection != "reject" && e.Status != "5");
            if (empid != null && empid != "")
            {




               
                if (designation == "HR")
                {
                    query = query.Where(e => (e.Status != "0" && e.Status != "1") || (e.EmpID == empid && e.Status == "4")|| (e.IRB == empid && e.Status != "0"));
                }
                else if (designation == "AGM")
                {
                    query = query.Where(e => (e.Status != "0" && e.Status != "1" && e.Status != "2") || (e.EmpID == empid && e.Status == "4")|| (e.IRB == empid && e.Status != "0"));
                }
                else if (designation == "ACCOUNTS")
                {
                    query = query.Where(e => (e.Status == "4" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid && e.Status != "4")|| (e.IRB == empid && e.Status != "0"));
                }
                else if (employee != null)
                {
                    query = query.Where(e => (e.IRB == empid && e.Status != "0") || (e.EmpID == empid && e.Status == "4")|| (e.IRB == empid && e.Status != "0"));
                }
                else
                {
                    query = query.Where(e => e.EmpID == empid && e.Status == "4");
                }

                var count = query
                    .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode, e.ExpenseID })
                    .Count();
                return Ok(count);
            }
            else
            {
                return Ok(0);
            }

        }






        // rejected count get form here 
        [HttpGet("rejected")]
        public IActionResult GetRejectedCount(string empid, string designation)
        {
            var empLocation = _context.EmployeeDetails
                     .Where(x => x.EmpID == empid)
                     .Select(x => x.CompanyLocation)
                     .FirstOrDefault();
            List<string> sameLocationEmpIds;

            if (empLocation == null)
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == null)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            else
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == empLocation)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            var employee = _context.ExpenseLogBook.FirstOrDefault(e => e.IRB == empid);
            IQueryable<ExpenseLogBook> query = _context.ExpenseLogBook.Where(e => e.Rejection == "reject" && e.Status != "5");
            if (empid != "null")
            {
               
                if (designation == "HR")
                {
                    query = query.Where(e => (e.Status == "1") || (e.EmpID == empid)|| (e.IRB == empid && e.Status == "0"));
                }
                else if (designation == "AGM")
                {
                    query = query.Where(e => (e.Status == "2") || (e.EmpID == empid)|| (e.IRB == empid && e.Status == "0"));
                }
                else if (designation == "ACCOUNTS")
                {
                    query = query.Where(e => (e.Status == "3" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid)|| (e.IRB == empid && e.Status == "0"));
                }
                else if (employee != null)
                {
                    query = query.Where(e => (e.IRB == empid && e.Status == "0") || (e.EmpID == empid));
                }
                else
                {
                    query = query.Where(e => e.EmpID == empid);
                }

                var count = query
                    .GroupBy(e => new { e.EmpID, e.SubmissionDate, e.ProjectCode, e.ExpenseID })
                    .Count();
                return Ok(count);
            }
            else
            {
                return Ok(0);
            }



        }



        // inside DashboardController
        [HttpGet("details")]
        public IActionResult GetGroupedDetails(string empid, string designation, string type, string search = "", int page = 1,
  int pageSize = 4)
        {
            // base filter
            var empLocation = _context.EmployeeDetails
                     .Where(x => x.EmpID == empid)
                     .Select(x => x.CompanyLocation)
                     .FirstOrDefault();
            List<string> sameLocationEmpIds;

            if (empLocation == null)
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == null)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            else
            {
                sameLocationEmpIds = _context.EmployeeDetails
                    .Where(x => x.CompanyLocation == empLocation)
                    .Select(x => x.EmpID)
                    .ToList();
            }
            var employee = _context.ExpenseLogBook.FirstOrDefault(e => e.IRB == empid);
            IQueryable<ExpenseLogBook> baseQuery = _context.ExpenseLogBook;

            // rejection logic:
            if (type == "rejected")
            {


                baseQuery = baseQuery.Where(e => e.Rejection == "reject" && e.Status != "5");

                if (!string.IsNullOrEmpty(empid))
                {




                    if (designation == "HR")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "1") || (e.EmpID == empid) || (e.IRB == empid && e.Status == "0"));
                    }
                    else if (designation == "AGM")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "2") || (e.EmpID == empid) || (e.IRB == empid && e.Status == "0"));
                    }
                    else if (designation == "ACCOUNTS")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "3" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid) || (e.IRB == empid && e.Status == "0"));
                    }
                    else if (employee != null)
                    {
                        baseQuery = baseQuery.Where(e => (e.IRB == empid && e.Status == "0") || (e.EmpID == empid));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(e => e.EmpID == empid);
                    }
                }
                else
                {


                    return Ok(new List<DashboardGroupDto>());

                }

            }

            else if (type == "approved")
            {
                baseQuery = baseQuery.Where(e => e.Rejection != "reject" && e.Status != "5");


                if (!string.IsNullOrEmpty(empid))
                {




                    if (designation == "HR")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status != "0" && e.Status != "1") || (e.EmpID == empid && e.Status == "4") || (e.IRB == empid && e.Status != "0"));
                    }
                    else if (designation == "AGM")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status != "0" && e.Status != "1" && e.Status != "2") || (e.EmpID == empid && e.Status == "4") || (e.IRB == empid && e.Status != "0"));
                    }
                    else if (designation == "ACCOUNTS")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "4" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid && e.Status == "4") || (e.IRB == empid && e.Status != "0"));
                    }
                    else if (employee != null)
                    {
                        baseQuery = baseQuery.Where(e => (e.IRB == empid && e.Status != "0") || (e.EmpID == empid && e.Status == "4"));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(e => e.EmpID == empid && e.Status == "4");
                    }
                }
                else
                {


                    return Ok(new List<DashboardGroupDto>());

                }

            }
            else if (type == "pending")
            {


                baseQuery = baseQuery.Where(e => e.Rejection != "reject" && e.Status != "4" && e.Status != "5");

                if (!string.IsNullOrEmpty(empid))
                {



                    if (designation == "HR")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "1") || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                    }
                    else if (designation == "AGM")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "2") || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                    }
                    else if (designation == "ACCOUNTS")
                    {
                        baseQuery = baseQuery.Where(e => (e.Status == "3" && sameLocationEmpIds.Contains(e.EmpID)) || (e.EmpID == empid && e.Status != "4") || (e.IRB == empid && e.Status == "0"));
                    }

                    else if (employee != null)
                    {
                        baseQuery = baseQuery.Where(e => (e.IRB == empid && e.Status == "0") || (e.EmpID == empid && e.Status != "4"));
                    }
                    else
                    {
                        baseQuery = baseQuery.Where(e => e.EmpID == empid && e.Status != "4");
                    }
                }
                else
                {


                    return Ok(new List<DashboardGroupDto>());

                }




            }
            else if (type == "checked")
            {


                baseQuery = baseQuery.Where(e => e.Rejection != "reject" && e.EmpID == empid && e.Status == "5");

            }
            else
            {
                return BadRequest("Invalid type parameter.");
            }


            // role-specific filtering (same logic as your counts)


            // group by EmpID + SubmissionDate (date only) + ProjectCode + SiteName

            var groupsQuery =
                from e in baseQuery
                join emp in _context.EmployeeDetails on e.EmpID equals emp.EmpID into empJoin
                from emp in empJoin.DefaultIfEmpty()
                join irbEmp in _context.EmployeeDetails on e.IRB equals irbEmp.EmpID into irbJoin
                from irbEmp in irbJoin.DefaultIfEmpty()
                group new { e, emp, irbEmp } by new
                {
                    e.EmpID,
                    e.ExpenseID,
                    SubmissionDate = e.SubmissionDate.HasValue ? e.SubmissionDate.Value.Date : (DateTime?)null,
                    e.ProjectCode,
                    e.SiteName,
                    e.IRB,
                    e.Status,
                    e.Rejection,
                    e.IRBApprovedDate,
                    e.HRApprovelDate,
                    e.AGMApprovelDate,
                    e.AccountApprovelDate
                } into g
                select new DashboardGroupDto
                {
                    EmpID = g.Key.EmpID,
                    ExpenseId = g.Key.ExpenseID,
                    SubmissionDate = g.Key.SubmissionDate,
                    ProjectCode = g.Key.ProjectCode,
                    SiteName = g.Key.SiteName,
                    IRB = g.Key.IRB,
                    Status = g.Key.Status,
                    Rejection = g.Key.Rejection,
                    TotalClaimAmount = g.Sum(x => x.e.ClaimAmount ?? 0),
                    TotalSanctionedAmount = g.Sum(x => x.e.SanctionedAmount ?? 0),
                    EmpName = g.Select(x => x.emp.EmpName).FirstOrDefault() ?? "",
                    IRBName = g.Select(x => x.irbEmp.EmpName).FirstOrDefault() ?? "",
                    IRBApprovedDate = g.Key.IRBApprovedDate,
                    HRApprovelDate = g.Key.HRApprovelDate,
                    AGMApprovelDate = g.Key.AGMApprovelDate,
                    AccountApprovelDate = g.Key.AccountApprovelDate
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                groupsQuery = groupsQuery.Where(x =>
                    x.EmpID.ToLower().Contains(search) ||
                    x.EmpName.ToLower().Contains(search) ||
                    //x.IRB.ToLower().Contains(search) ||
                    //x.IRBName.ToLower().Contains(search) ||
                    x.ProjectCode.ToLower().Contains(search) ||
                    x.SiteName.ToLower().Contains(search) ||
                    x.ExpenseId.ToString().Contains(search)
                );
            }

            var totalCount = groupsQuery.Count();
            // Apply ordering based on type
            IQueryable<DashboardGroupDto> orderedQuery = type switch
            {
                "approved" => groupsQuery.OrderByDescending(x => x.ExpenseId),
                "rejected" => groupsQuery.OrderBy(x => x.ExpenseId),
                "pending" => groupsQuery.OrderBy(x => x.ExpenseId),
                "checked" => groupsQuery.OrderBy(x => x.ExpenseId),
                _ => groupsQuery.OrderBy(x => x.ExpenseId)
            };

            var pagedData = orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                data = pagedData
            });

          
        }







        [HttpGet("groupitems")]
        public async Task<IActionResult> GetGroupItems(string empid, DateTime submissionDate, long expenseId, string type)
        {
            // Normalize empid
            var status = type.Substring(0, 1);
            var types = type.Substring(1);

            empid = empid.Trim();
            IQueryable<ExpenseLogBook> baseQuery = _context.ExpenseLogBook;
            if (types == "rejected")
            {
                baseQuery = baseQuery.Where(e => e.EmpID == empid
                             && e.ExpenseID == expenseId
                             && e.SubmissionDate.HasValue
                             && e.Rejection == "reject"
                             && e.Status == status
                             && e.SubmissionDate.Value.Date == submissionDate.Date);
            }
            else
            {
                baseQuery = baseQuery.Where(e => e.EmpID == empid
                           && e.ExpenseID == expenseId
                           && e.SubmissionDate.HasValue
                           && e.Rejection != "reject"
                            && e.Status == status
                           && e.SubmissionDate.Value.Date == submissionDate.Date);
            }

            var items = await baseQuery
                .OrderBy(e => e.DateofExpense) // ✅ ascending order by date
                                               // .OrderByDescending(e => e.DateofExpense) // 👈 use this if you want latest first
                .Select(e => new ExpenseItemDto
                {
                    ID = e.ID,
                    DateOfExpense = e.DateofExpense,
                    TypeOfExpense = e.TypeOfExpense,
                    TravelLocation = e.TravelLocation,
                    Quantity = e.Quantity,
                    FellowMembers = e.FellowMembers,
                    ClaimAmount = e.ClaimAmount,
                    SanctionedAmount = e.SanctionedAmount,
                    BillDocument = e.BillDocument,
                    RequireSpecialApproval = e.RequireSpecialApproval,
                    Status = e.Status,
                    Rejection = e.Rejection,
                    Reason = e.Reason,
                    SiteName = e.SiteName

                })
                .ToListAsync();

            return Ok(items);
        }




        //        public class UpdateGroupActionDto
        //    {
        //        public string EmpID { get; set; } = "";
        //        public DateTime SubmissionDate { get; set; }
        //        public string ProjectCode { get; set; } = "";
        //        public string ActionBy { get; set; } = "";
        //            public long ExpenseId { get; set; } = 0;// EmpID who clicked accept/reject
        //}

        //// Accept (finalize) group - increments status to next stage or to 4 for accounts final
        //[HttpPost("accept-group")]
        //        public IActionResult AcceptGroup([FromBody] UpdateGroupActionDto dto)
        //        {
        //            var rows = _context.ExpenseLogBook
        //                .Where(e => e.EmpID == dto.EmpID
        //                         && e.ProjectCode == dto.ProjectCode
        //                         && e.SubmissionDate.HasValue
        //                         && e.SubmissionDate.Value.Date == dto.SubmissionDate.Date)
        //                .ToList();

        //            if (!rows.Any()) return NotFound();

        //            foreach (var r in rows)
        //            {
        //                // increment status number (stored as string)
        //                int st = 0;
        //                int.TryParse(r.Status ?? "0", out st);
        //                r.Status = (st + 1).ToString();
        //                r.Rejection = null; // clear rejection when accepted
        //            }
        //            _context.SaveChanges();
        //            return Ok();
        //        }

        //        // Reject group - set Rejection = "reject" and optionally increment status
        //        [HttpPost("reject-group")]
        //        public IActionResult RejectGroup([FromBody] UpdateGroupActionDto dto)
        //        {
        //            var rows = _context.ExpenseLogBook
        //                .Where(e => e.EmpID == dto.EmpID
        //                         && e.ProjectCode == dto.ProjectCode
        //                         && e.ExpenseID==dto.ExpenseId
        //                         && e.SubmissionDate.HasValue
        //                         && e.SubmissionDate.Value.Date == dto.SubmissionDate.Date)
        //                .ToList();

        //            if (!rows.Any()) return NotFound();

        //            foreach (var r in rows)
        //            {
        //                r.Rejection = "reject";
        //                // optionally increment status:
        //                int st = 0;
        //                int.TryParse(r.Status ?? "0", out st);
        //                r.Status = (st ).ToString();
        //            }
        //            _context.SaveChanges();
        //            return Ok();
        //        }

        [HttpPost("update-claim-amount")]
        public IActionResult UpdateClaimAmount([FromBody] UpdateClaimAmountRequest request)
        {
            if (request?.Rows == null || !request.Rows.Any())
                return BadRequest(new { success = false, message = "No items to update." });

            foreach (var row in request.Rows)
            {
                var item = _context.ExpenseLogBook.FirstOrDefault(x => x.ID == row.ItemId);
                if (item != null)
                {
                    item.ClaimAmount = row.NewClaimAmount;
                    // Optionally, you can also update SanctionedAmount if needed
                    // item.SanctionedAmount = row.NewClaimAmount;
                }
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Claim amounts updated successfully." });
        }


        public class UpdateClaimAmountRequest
        {
            public long ExpenseId { get; set; }  // ExpenseBillBook ID
            public List<ClaimAmountRow> Rows { get; set; }
        }

        public class ClaimAmountRow
        {
            public long ItemId { get; set; }    // ExpenseLogBook.ID
            public double NewClaimAmount { get; set; }
        }




        //accepting the particular group item

        [HttpPost("accept-group")]
        public async Task<IActionResult> AcceptGroup([FromBody] AcceptGroupRequest req)
        {
            var empAcceptId = User.FindFirst("EmpID")?.Value;
           var designation=User.FindFirst("Designation")?.Value;
            var billempname = "";
            var empId = "";
            var empName = "";
            var site = "";
            var project = "";
            var irb = "";
            var status = "";
            double billamount = 0;

            var items = await _context.ExpenseLogBook
                .Where(e => e.EmpID == req.EmpID
                         && e.SubmissionDate == req.SubmissionDate.Date
                         && e.ProjectCode == req.ProjectCode
                         && e.ExpenseID == req.ExpenseId)
                .ToListAsync();

            string? emailToSend = null;
            MimeMessage? emailMessage = null;

            foreach (var row in req.Rows)
            {
                var item = items.FirstOrDefault(i => i.ID == row.ItemId);
                if (item == null)
                    continue;

              
               
                item.Rejection = row.IsRejected ? "reject" : null;
                item.Reason = string.IsNullOrWhiteSpace(row.Reason) ? null : row.Reason;
                empName = User.FindFirst("EmpName")?.Value;
                status = item.Status;
                site = item.SiteName;
                project = item.ProjectCode;
                irb = item.IRB;
                empId = item.EmpID;
                if (empId == empAcceptId && item.Rejection == "reject")
                {
                    item.Rejection = null;
                    item.Reason = null;
                    if (item.Status == "0" || item.Status == "5")
                    {
                      
                    }
                    else
                    {
                        item.Status = "1";
                   
                    }
                }
                if (!row.IsRejected)
                {
                    int currentStatus = 0;

                    if (item.Status == "5")
                    {

                        item.Status = "0";
                        item.SubmissionDate= DateTime.Now;

                        // ===============================
                        // Prepare EMAIL — but do NOT send yet
                        // ===============================





                    }
                    else
                    {


                        // Approval Section
                        // Approval Section
                        if (item.Status == "1")
                        {
                            if (designation != "HR")
                            {
                                return BadRequest(new { success = false });
                            }

                            item.HRApprovel = empAcceptId;
                            item.HRApprovelDate = DateTime.Now;
                            item.SanctionedAmount = row.NewSanctionedAmount;
                            item.RequireSpecialApproval = row.IsSpecial ? "false" : $"{row.IsSpecial}";

                        }
                        else if (item.Status == "2")
                        {
                            if (designation != "AGM")
                            {
                                return BadRequest(new { success = false });
                            }
                            item.AGMApprovel = empAcceptId;
                            item.AGMApprovelDate = DateTime.Now;
                            item.SanctionedAmount = row.NewSanctionedAmount;
                            item.RequireSpecialApproval = row.IsSpecial ? "false" : $"{row.IsSpecial}";

                        }
                        else if (item.Status == "3")
                        {
                            if (designation != "ACCOUNTS")
                            {
                                return BadRequest(new { success = false });
                            }
                            item.AccountApprovel = empAcceptId;
                            item.AccountApprovelDate = DateTime.Now;
                            billamount += Convert.ToDouble(item.SanctionedAmount);


                        }
                        else if (item.Status == "0")
                        {
                            if (empAcceptId != item.IRB)
                            {
                                return BadRequest(new { success = false });
                            }
                            item.IRBApprovedDate = DateTime.Now;


                        }
                        else if (item.Status == "4")
                        {
                            return BadRequest(new { success = false });
                        }
                        int.TryParse(item.Status, out currentStatus);
                        currentStatus++;
                        item.Status = currentStatus.ToString();
                    }
                }
            }
            if (status == "5")
            {
                try
                {
                    _context.Notifications.Add(new Notification
                    {
                        EmpID = irb,
                        Title = "New Bill",
                        Message = $"New bill filled by {empAcceptId} for the {site}",
                        Type = "Reimbursement bill"
                    });
                    emailToSend = await _context.EmployeeDetails
                           .Where(e => e.EmpID == irb)
                           .Select(e => e.MailID)
                           .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(emailToSend))
                    {
                        emailMessage = new MimeMessage();
                        emailMessage.From.Add(new MailboxAddress("PMAL Control", _emailSettings.Email));
                        emailMessage.To.Add(new MailboxAddress("", emailToSend));
                        emailMessage.Subject = "Bill Request";

                        emailMessage.Body = new TextPart("plain")
                        {
                            Text = $"Hello,\n\n {empId} \n  {empName} \n{site}\n {project}\n Please approve the bill.\n\nRegards,\n{empName}\nPMAL Control"
                        };
                    }
                    // ===============================
                    // Send email AFTER everything saves
                    // ===============================
                    if (emailMessage != null)
                    {
                        using var client = new SmtpClient();
                        await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                        await client.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);
                        await client.SendAsync(emailMessage);
                        await client.DisconnectAsync(true);
                    }

                }
                catch
                {
                    // Log error or handle accordingly
                }


            }
            else if (status == "0")
            {
                var nextempid = await _context.EmployeeDetails
                           .Where(e => e.Designation == "HR")
                           .Select(e => e.EmpID)
                           .FirstOrDefaultAsync();
                _context.Notifications.Add(new Notification
                {
                    EmpID = empId,
                    Title = "Bill approved",
                    Message = $" bill Approved by {empAcceptId}",
                    Type = "Reimbursement bill"
                });
                _context.Notifications.Add(new Notification
                {
                    EmpID = nextempid,
                    Title = "New Bill",
                    Message = $"New bill Approved by {empAcceptId}",
                    Type = "Reimbursement bill"
                });

            }
            else if (status == "1")
            {
                var nextempid = await _context.EmployeeDetails
                           .Where(e => e.Designation == "AGM")
                           .Select(e => e.EmpID)
                           .FirstOrDefaultAsync();
                _context.Notifications.Add(new Notification
                {
                    EmpID = empId,
                    Title = "Bill approved",
                    Message = $" bill Approved by {empAcceptId}",
                    Type = "Reimbursement bill"
                });
                _context.Notifications.Add(new Notification
                {
                    EmpID = nextempid,
                    Title = "New Bill",
                    Message = $"New bill Approved by {empAcceptId}",
                    Type = "Reimbursement bill"
                });

            }
            else if (status == "2")
            {
                var nextempid = await _context.EmployeeDetails
                           .Where(e => e.Designation == "ACCOUNTS")
                           .Select(e => e.EmpID)
                           .FirstOrDefaultAsync();
                _context.Notifications.Add(new Notification
                {
                    EmpID = empId,
                    Title = "Bill approved",
                    Message = $" bill Approved by HR ADMIN",
                    Type = "Reimbursement bill"
                });
                _context.Notifications.Add(new Notification
                {
                    EmpID = nextempid,
                    Title = "New Bill",
                    Message = $"New bill Approved by {empAcceptId}",
                    Type = "Reimbursement bill"
                });

            }
            else if (status == "3")
            {
                billempname = await _context.EmployeeDetails
    .Where(e => e.EmpID == empId)
    .Select(e => e.EmpName)
    .FirstOrDefaultAsync();

                _context.Notifications.Add(new Notification
                {
                    EmpID = empId,
                    Title = "Bill approved",
                    Message = $" bill Approved by ACCOUNTS",
                    Type = "Reimbursement bill"
                });
                var employeeAccount = new EmployeeAccount
                {
                    EMP_ID = empId,
                    EMP_NAME = billempname,
                    ADVANCE_AMOUNT = billamount,
                    PAYMENT_TYPE = "BILL APPROVED",
                    APPROVAL_EMP = empAcceptId,
                    DATETIME = DateTime.Now,
                    PROJECT_CODE = project
                };

                _context.EmployeeAccount.Add(employeeAccount);


            }

            // ===============================
            // Save database changes first
            // ===============================
            await _context.SaveChangesAsync();
            //for sending the male


            return Ok(new { success = true });
        }

     



        [HttpGet("report")]
        public async Task<IActionResult> GetExpenseReport(DateTime from, DateTime to)
        {
            var data = await _context.ExpenseLogBook
                .Where(x => x.DateofExpense >= from && x.DateofExpense <= to)
                .ToListAsync();

            return Ok(data); // frontend filters by status and rejection
        }


        // Reject all
        [HttpPost("reject-group")]
        public IActionResult RejectGroup([FromBody] RejectGroupRequest req)
        {
            var empAcceptId = User.FindFirst("EmpID")?.Value;
            var designation = User.FindFirst("Designation")?.Value;
            var empAcceptName = User.FindFirst("EmpName")?.Value;
            var status1 = "";
            var irb1 = "";
            var items = _context.ExpenseLogBook
                .Where(e => e.EmpID == req.EmpID
                         && e.SubmissionDate == req.SubmissionDate.Date
                         && e.ProjectCode == req.ProjectCode
                         && e.ExpenseID == req.ExpenseId)
                .ToList();

            foreach (var item in items)
            {
                item.Rejection = "reject";
                item.Reason = req.Reason;
                status1 = item.Status;
                irb1 = item.IRB;
            }
            _context.Notifications.Add(new Notification
            {
                EmpID = req.EmpID,
                Title = "New Bill",
                Message = $"New bill from Rejected by {empAcceptName} of submission date {req.SubmissionDate}",
                Type = "Reimbursement bill"
            });
            if (status1=="0")
            {
                if (empAcceptId != irb1)
                {
                    return BadRequest(new { success = false });
                }
            }
            else if (status1 == "1")
            {
                if (designation != "HR")
                {
                    return BadRequest(new { success = false });
                }
            }
            else if (status1 == "2")
            {
                if (designation != "AGM")
                {
                    return BadRequest(new { success = false });
                }
            }
            else if (status1 == "3")
            {
                if (designation != "ACCOUNTS")
                {
                    return BadRequest(new { success = false });
                }
            }
            else if (status1 == "4")
            {
                return BadRequest(new { success = false });
            }





            _context.SaveChanges();
            return Ok(new { success = true });
        }

        public class SpecialApproveDto
        {
            public long ItemId { get; set; }
            public double NewSanctionedAmount { get; set; }
            public string ActionBy { get; set; } = "";
        }

        // Special approve single item: replace sanctioned amount with claim amount (or given amount)
        [HttpPost("special-approve")]
        public IActionResult SpecialApprove([FromBody] SpecialApproveDto dto)
        {
            var item = _context.ExpenseLogBook.FirstOrDefault(e => e.ID == dto.ItemId);
            if (item == null) return NotFound();

            // validate permission on server side if required (based on ActionBy)
            item.SanctionedAmount = dto.NewSanctionedAmount;
            item.RequireSpecialApproval = "true";
            _context.SaveChanges();
            return Ok();
        }

        [HttpGet("GetBillInfo/{id}")]
        public IActionResult GetBillInfo(int id)
        {
            var bill = _context.ExpenseLogBook.FirstOrDefault(e => e.ID == id);
            if (bill == null || bill.BillDocument == null)
                return NotFound(new { message = "File not found" });

            // Generate a file access URL for the viewer
            var fileUrl = Url.Action("ViewBill", "Expenses", new { id = id }, Request.Scheme);
            return Ok(new
            {
                fileUrl,
                contentType = bill.BillContentType ?? "application/octet-stream",
                fileName = bill.BillFileName ?? $"bill_{id}"
            });
        }



        [HttpGet]
        [Route("/Expenses/ViewBill/{id:long}")]
        public IActionResult ViewBill(int id)
        {
            var bill = _context.ExpenseLogBook.FirstOrDefault(e => e.ID == id);
            if (bill == null || bill.BillDocument == null)
                return NotFound();

            // Just return file for inline viewing
            return File(bill.BillDocument, bill.BillContentType ?? "application/octet-stream");
        }


        // DELETE: api/dashboard/delete-expense/5
        [HttpDelete("delete-expense/{id:long}")]
        public async Task<IActionResult> DeleteExpense(long id)
        {
            var expense = await _context.ExpenseBillBook.FindAsync(id);
            if (expense == null)
                return NotFound(new { message = "Expense not found" });
           


                _context.ExpenseBillBook.Remove(expense);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Expense deleted successfully" });
        }

        // ✅ NEW ENDPOINT for Impress Pending Count
        [HttpGet("impress-pending")]
        public async Task<int> GetImpressPending()
        {
            var location = User.FindFirst("CompanyLocation")?.Value;

            var emplist = _context.EmployeeDetails
                                  .Where(x => x.CompanyLocation == location)
                                  .Select(x => x.EmpID)
                                  .ToList();
            var count = await _context.EmployeeImpress
                .CountAsync(e => e.STATUS == "pending" && emplist.Contains(e.EmpID));
            return count;
        }
        //imress pending for agm
        [HttpGet("impress-pendingagm")]
        public async Task<int> GetImpressPendingForAgm()
        {
            var location = User.FindFirst("CompanyLocation")?.Value;

            var emplist = _context.EmployeeDetails
                                  .Where(x => x.CompanyLocation == location)
                                  .Select(x => x.EmpID)
                                  .ToList();
            var count = await _context.EmployeeImpress
                .CountAsync(e => e.STATUS == "inProcess" && emplist.Contains(e.EmpID));
            return count;
        }
        //approved amount
        //total approved 
        [HttpGet("Total-approved")]
        public async Task<double> GetTotalApprove()
        {
            var totalSanctioned = await _context.ExpenseLogBook
                .Where(e => e.Status == "4")
                .SumAsync(e => e.SanctionedAmount ?? 0);

            return totalSanctioned;
        }


        // ✅ NEW ENDPOINT for Impress Pending Count
        [HttpGet("impress-pendingaccounts")]
        public async Task<int> GetImpressPendingFromHr()
        {
            var location = User.FindFirst("CompanyLocation")?.Value;

            var emplist = _context.EmployeeDetails
                                  .Where(x => x.CompanyLocation == location)
                                  .Select(x => x.EmpID)
                                  .ToList();
            var count = await _context.EmployeeAccount
                .CountAsync(e => e.IMPRESS_AMOUNT != null && emplist.Contains(e.EMP_ID));
            return count;
        }
        // Check for similar expenses

        [HttpGet("check-similar")]
        public IActionResult CheckSimilarExpense(string empId, string typeOfExpense, DateTime dateOfExpense, long expensebill)
        {
            var startDate = dateOfExpense.Date;
            var endDate = startDate.AddDays(1);

            var data = _context.ExpenseLogBook
                .Where(e =>
                    e.TypeOfExpense == typeOfExpense &&
                    e.ExpenseID != expensebill &&
                    e.DateofExpense >= startDate && e.DateofExpense < endDate &&
                    (
                        e.EmpID == empId ||
                        (e.FellowMembers != null && e.FellowMembers.Contains(empId))
                    )
                )
                .Select(e => new
                {
                    e.TypeOfExpense,
                    e.EmpID,
                    e.FellowMembers,
                    e.TravelLocation,
                    e.SiteName,
                    e.ProjectCode,
                    e.ClaimAmount
                })
                .ToList();

            // ✅ Only return data if count > 1

            return Ok(data);

        }


        //check hide button
        // Check for similar expenses

        [HttpGet("check-similarHide")]
        public IActionResult CheckSimilarExpenseHide(string empId, string typeOfExpense, DateTime dateOfExpense, long billID)
        {
            var startDate = dateOfExpense.Date;
            var endDate = startDate.AddDays(1);

            var data = _context.ExpenseLogBook
                .Where(e =>
                    e.TypeOfExpense == typeOfExpense &&
                    e.ExpenseID != billID &&
                    e.DateofExpense >= startDate && e.DateofExpense < endDate &&
                    (
                        e.EmpID == empId ||
                        (e.FellowMembers != null && e.FellowMembers.Contains(empId))
                    )
                )
                .Select(e => new
                {
                    e.TypeOfExpense,
                    e.EmpID,
                    e.FellowMembers,
                    e.TravelLocation,
                    e.SiteName,
                    e.ProjectCode
                })
                .ToList();

            // ✅ Only return data if count > 1

            return Ok(data);

        }
        //expesne edit we send the limit from that api 

        //[HttpGet("getallexpenselimit")]
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


        // ✅ GET: /api/Dashboard/getallexpenselimit
        [HttpGet("getallexpenselimit")]
        public async Task<ActionResult<IEnumerable<ExpenseLimitDetails>>> GetAllExpenseLimit()
        {
            var limits = await _context.ExpenseLimitDetails.ToListAsync();

            if (limits == null || limits.Count == 0)
                return NotFound("No expense limits found.");

            return Ok(limits);
        }

        //edit expense api 
        [HttpGet("groupedit")]
        public async Task<IActionResult> GetGroupEdit(long expenseID)
        {
            var empID = User.FindFirst("EmpID")?.Value;
            if (empID == null)
                return Unauthorized();

            var employee = await _context.EmployeeDetails
                .FirstOrDefaultAsync(e => e.EmpID == empID);

            var typeList = await _context.TypeOfExpense
                .Select(t => new { t.TypeOfExpense })
                .ToListAsync();

            var employeeList = await _context.EmployeeDetails
                .Select(e => new { e.EmpID, e.EmpName })
                .ToListAsync();

            var expenses = await _context.ExpenseLogBook
                .Where(x => x.ExpenseID == expenseID)
                .Select(x => new
                {
                    x.ID,
                    x.ExpenseID,
                    x.TypeOfExpense,
                    x.TravelLocation,
                    x.Quantity,
                    x.FellowMembers,
                    x.ClaimAmount,
                    x.SanctionedAmount,
                    x.SubmissionDate,
                    x.ProjectCode,
                    x.RequireSpecialApproval
                })
                .ToListAsync();

            return Ok(new
            {
                Employee = new
                {
                    employee.EmpID,
                    employee.EmpName,
                    employee.Level
                },
                TypeList = typeList,
                EmployeeList = employeeList,
                Expenses = expenses
            });
        }


        //[HttpGet("groupedit")]
        //public async Task<IActionResult> GetGroupEdit(string empId, DateTime submissionDate, string projectCode, long expenseId)
        //{
        //    try
        //    {
        //        // 1️⃣ Fetch employee info
        //        var employee = await _context.EmployeeDetails
        //            .Where(e => e.EmpID == empId)
        //            .Select(e => new
        //            {
        //                e.EmpID,
        //                e.EmpName,
        //                e.Designation,
        //                e.Level,
        //                e.Dept
        //            })
        //            .FirstOrDefaultAsync();

        //        if (employee == null)
        //            return NotFound("Employee not found");

        //        // 2️⃣ Get expenses by ExpenseID
        //        var expenses = await _context.ExpenseLogBook
        //            .Where(e => e.EmpID == empId &&
        //                        e.SubmissionDate == submissionDate &&
        //                        e.ProjectCode == projectCode &&
        //                        e.ExpenseID == expenseId)
        //            .Select(e => new
        //            {
        //                e.ID,
        //                e.ExpenseID,
        //                e.TypeOfExpense,
        //                e.DateofExpense,
        //                e.TravelLocation,
        //                e.Quantity,
        //                e.FellowMembers,
        //                e.ClaimAmount,
        //                e.SanctionedAmount,
        //                e.RequireSpecialApproval,
        //                e.BillFileName,
        //                HasBill = e.BillDocument != null
        //            })
        //            .ToListAsync();

        //        // 3️⃣ Get dropdowns
        //        var typeList = await _context.TypeOfExpense
        //            .Select(t => new { t.TypeOfExpense })
        //            .ToListAsync();

        //        var employeeList = await _context.EmployeeDetails
        //            .Select(e => new { e.EmpID, e.EmpName })
        //            .ToListAsync();

        //        // 4️⃣ Build result
        //        return Ok(new
        //        {
        //            Employee = employee,
        //            ProjectCode = projectCode,
        //            SubmissionDate = submissionDate.ToString("yyyy-MM-dd"),
        //            ExpenseID = expenseId,
        //            TypeList = typeList,
        //            EmployeeList = employeeList,
        //            Expenses = expenses
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { error = ex.Message });
        //    }
        //}




    }

    public class AcceptGroupRequest
    {
        public string EmpID { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string ProjectCode { get; set; }
        public string ActionBy { get; set; }
        public long ExpenseId { get; set; }
        public string Designation { get; set; }
        public List<RowUpdate> Rows { get; set; }
    }

    public class RowUpdate
    {
        public long ItemId { get; set; }
        public bool IsRejected { get; set; }
        public bool IsSpecial { get; set; }
        public double NewSanctionedAmount { get; set; }
        public string? Reason { get; set; }
    }

    public class RejectGroupRequest
    {
        public string EmpID { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string ProjectCode { get; set; }
        public string ActionBy { get; set; }
        public long ExpenseId { get; set; }
        public string? Reason { get; set; }
    }
}
