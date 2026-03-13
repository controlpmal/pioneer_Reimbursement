using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;

public class ExpenseLimitDetailsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpenseLimitDetailsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.ExpenseLimitDetails.ToListAsync());
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseLimitDetails model)
    {
        if (ModelState.IsValid)
        {
            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public async Task<IActionResult> Edit(long id)
    {
        var data = await _context.ExpenseLimitDetails.FindAsync(id);
        return View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ExpenseLimitDetails model)
    {
        if (ModelState.IsValid)
        {
            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public async Task<IActionResult> Delete(long id)
    {
        var data = await _context.ExpenseLimitDetails.FindAsync(id);
        return View(data);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var data = await _context.ExpenseLimitDetails.FindAsync(id);
        _context.Remove(data);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
