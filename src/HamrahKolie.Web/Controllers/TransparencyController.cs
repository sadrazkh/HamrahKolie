using HamrahKolie.Application.Reports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Controllers;

/// <summary>گزارش عمومی شفافیت مؤسسه.</summary>
public class TransparencyController : Controller
{
    private readonly IReportService _reports;
    public TransparencyController(IReportService reports) => _reports = reports;

    [HttpGet("/transparency")]
    [OutputCache(Duration = 300)]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "شفافیت مالی";
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}/transparency";
        var report = await _reports.GetTransparencyReportAsync();
        return View(report);
    }
}
