using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.ContentEdit)]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISlugService _slug;

    public CategoriesController(ApplicationDbContext db, ISlugService slug)
    {
        _db = db;
        _slug = slug;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "دسته‌بندی‌ها";
        var cats = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug, Count = c.Contents.Count })
            .ToListAsync();
        return View(cats.Select(c => (c.Id, c.Name, c.Slug, c.Count)).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "نام دسته را وارد کنید.";
            return RedirectToAction(nameof(Index));
        }

        var slug = await _slug.GenerateUniqueAsync(name,
            candidate => _db.Categories.AnyAsync(c => c.Slug == candidate));

        _db.Categories.Add(new Category { Name = name.Trim(), Slug = slug, Description = description?.Trim() });
        await _db.SaveChangesAsync();
        TempData["Success"] = "دسته‌بندی افزوده شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (cat is null) return NotFound();
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = "دسته‌بندی حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}
