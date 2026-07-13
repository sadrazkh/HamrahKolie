using HamrahKolie.Application.Volunteers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HamrahKolie.Web.Controllers;

[Route("volunteer")]
public class VolunteerController : Controller
{
    private readonly IVolunteerService _service;
    public VolunteerController(IVolunteerService service) => _service = service;

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "همکاری داوطلبانه";
        return View(new VolunteerInput());
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Index(VolunteerInput input)
    {
        ViewData["Title"] = "همکاری داوطلبانه";
        if (!ModelState.IsValid) return View(input);

        await _service.SubmitAsync(input);
        TempData["Success"] = "درخواست همکاری شما ثبت شد. کارشناسان ما با شما در تماس خواهند بود.";
        return RedirectToAction(nameof(Thanks));
    }

    [HttpGet("thanks")]
    public IActionResult Thanks()
    {
        ViewData["Title"] = "سپاسگزاریم";
        return View();
    }
}
