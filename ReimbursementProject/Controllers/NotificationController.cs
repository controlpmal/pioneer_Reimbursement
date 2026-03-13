using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;

[Authorize]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetUnreadNotifications()
    {
        var empId = User.FindFirst("EmpID")?.Value;

        var notifications = _context.Notifications
            .Where(n => n.EmpID == empId && n.IsRead==false)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Message })
            .ToList();

        return Json(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notif = await _context.Notifications.FindAsync(id);
        if (notif != null)
        {
            notif.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return Ok();
    }
    //clear all the notification controller
    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var empId = User.FindFirst("EmpID")?.Value;
        var notifications = await _context.Notifications
            .Where(n => n.EmpID == empId && n.IsRead == false)
            .ToListAsync();

        notifications.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
