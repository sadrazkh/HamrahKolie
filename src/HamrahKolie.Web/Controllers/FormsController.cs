using HamrahKolie.Application.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HamrahKolie.Web.Controllers;

/// <summary>نمایش و ثبت فرم‌های داینامیک ساخته‌شده در فرم‌ساز.</summary>
public class FormsController : Controller
{
    private readonly IFormService _forms;
    public FormsController(IFormService forms) => _forms = forms;

    [HttpGet("/forms/{slug}")]
    public async Task<IActionResult> Show(string slug)
    {
        var form = await _forms.GetEnabledBySlugAsync(slug);
        if (form is null) return NotFound();
        ViewData["Title"] = form.Title;
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
}
