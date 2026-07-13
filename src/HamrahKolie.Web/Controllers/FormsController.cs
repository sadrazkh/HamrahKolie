using HamrahKolie.Application.Forms;
using HamrahKolie.Application.PageBuilder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HamrahKolie.Web.Controllers;

/// <summary>نمایش و ثبت فرم‌های داینامیک ساخته‌شده در فرم‌ساز.</summary>
public class FormsController : Controller
{
    private readonly IFormService _forms;
    private readonly IPageBuilderService _pageBuilder;
    public FormsController(IFormService forms, IPageBuilderService pageBuilder)
    {
        _forms = forms;
        _pageBuilder = pageBuilder;
    }

    [HttpGet("/forms/{slug}")]
    public async Task<IActionResult> Show(string slug)
    {
        var form = await _forms.GetEnabledBySlugAsync(slug);
        if (form is null) return NotFound();
        ViewData["Title"] = form.Title;
        await LoadPageSectionsAsync(slug);
        return View(form);
    }

    [HttpPost("/forms/{slug}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Submit(string slug)
    {
        var form = await _forms.GetEnabledBySlugAsync(slug);
        if (form is null) return NotFound();
        ViewData["Title"] = form.Title;
        await LoadPageSectionsAsync(slug);

        var values = Request.Form
            .Where(kv => kv.Key != "__RequestVerificationToken")
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _forms.SubmitAsync(slug, values, ip);

        if (result.Success)
        {
            ViewData["SuccessMessage"] = result.SuccessMessage;
            return View("Show", form);
        }

        ViewData["Errors"] = result.Errors;
        ViewData["Values"] = values;
        return View("Show", form);
    }

    private async Task LoadPageSectionsAsync(string slug)
    {
        var pageKey = slug.Equals("contact", StringComparison.OrdinalIgnoreCase) ? "contact" : $"form:{slug.ToLowerInvariant()}";
        try { ViewData["PageBuilderSections"] = await _pageBuilder.GetVisibleAsync(pageKey); }
        catch { }
    }
}
