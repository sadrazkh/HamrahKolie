using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Reports;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.ReportView)]
public class ReportsController : Controller
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "گزارش‌ها";
        var report = await _reports.GetManagementReportAsync();
        return View(report);
    }
}
