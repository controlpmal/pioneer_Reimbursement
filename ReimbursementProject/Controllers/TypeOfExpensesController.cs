using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReimbursementProject.Data;
using ReimbursementProject.Models;

public class TypeOfExpensesController : Controller
{
    private readonly ApplicationDbContext _context;

    public TypeOfExpensesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.TypeOfExpense.ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TypeOfExpenses model)
    {
        if (ModelState.IsValid)
        {
            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var data = await _context.TypeOfExpense.FindAsync(id);
        return data == null ? NotFound() : View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TypeOfExpenses model)
    {
        if (ModelState.IsValid)
        {
            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        var data = await _context.TypeOfExpense.FindAsync(id);
        return View(data);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var data = await _context.TypeOfExpense.FindAsync(id);
        _context.TypeOfExpense.Remove(data);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
