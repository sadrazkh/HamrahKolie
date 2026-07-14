using HamrahKolie.Application.Centers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace HamrahKolie.Web.Controllers;

public class CentersController : Controller
{
    private const int PageSize = 12;
    private readonly ICenterService _centers;

    public CentersController(ICenterService centers) => _centers = centers;

    [HttpGet("/centers")]
    [OutputCache(PolicyName = "PublicContent")]
    public async Task<IActionResult> Index(string? province, string? search, int page = 1)
    {
        ViewData["Title"] = "مراکز دیالیز";
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}/centers";
        ViewData["Provinces"] = await _centers.GetProvincesAsync();
        ViewData["Province"] = province;
        ViewData["Search"] = search;
        var result = await _centers.GetApprovedListAsync(province, search, page, PageSize);
        return View(result);
    }

    [HttpGet("/centers/{slug}")]
    [OutputCache(PolicyName = "PublicContent")]
    public async Task<IActionResult> Detail(string slug)
    {
        var center = await _centers.GetApprovedBySlugAsync(slug);
        if (center is null) return NotFound();
        ViewData["Title"] = center.Name;
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        return View(center);
    }

    [HttpGet("/centers/submit")]
    public IActionResult Submit()
    {
        ViewData["Title"] = "پیشنهاد مرکز دیالیز";
        return View(new CenterInput());
    }

    [HttpPost("/centers/submit")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Submit(CenterInput input)
    {
        ViewData["Title"] = "پیشنهاد مرکز دیالیز";
        if (!ModelState.IsValid) return View(input);

        await _centers.SubmitPublicAsync(input);
        TempData["Success"] = "پیشنهاد شما ثبت شد و پس از بررسی توسط کارشناسان منتشر می‌شود.";
        return RedirectToAction(nameof(Index));
    }
}
