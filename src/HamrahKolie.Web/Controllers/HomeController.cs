using System.Diagnostics;
using HamrahKolie.Application.Cms;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Controllers;

/// <summary>
/// صفحات عمومی سایت. صفحه اصلی و فهرست‌ها از سیستم مدیریت محتوا تغذیه می‌شوند.
/// </summary>
public class HomeController : Controller
{
    private readonly IContentService _content;

    public HomeController(IContentService content) => _content = content;

    [OutputCache(Duration = 60)]
    public async Task<IActionResult> Index()
    {
        var vm = new HamrahKolie.Web.ViewModels.HomeViewModel();
        try
        {
            vm.LatestNews = await _content.GetLatestPublishedAsync(ContentType.News, 3);
            vm.LatestArticles = await _content.GetLatestPublishedAsync(ContentType.Article, 3);
        }
        catch
        {
            // اگر پایگاه داده در دسترس نباشد، صفحه اصلی بدون فهرست مطالب نمایش داده می‌شود.
        }
        return View(vm);
    }

    public IActionResult About()
    {
        ViewData["Title"] = "درباره مؤسسه";
        return View();
    }

    public IActionResult Services()
    {
        ViewData["Title"] = "خدمات حمایتی";
        return View();
    }

    public IActionResult Campaigns()
    {
        ViewData["Title"] = "کمپین‌ها";
        return View();
    }

    public IActionResult Donate()
    {
        ViewData["Title"] = "حمایت مالی";
        return View();
    }

    public IActionResult Contact()
    {
        ViewData["Title"] = "تماس با ما";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>صفحه خطاهای وضعیت HTTP (مثل 404).</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        ViewData["StatusCode"] = code;
        ViewData["Title"] = code == 404 ? "صفحه پیدا نشد" : "خطا";
        return View();
    }
}
