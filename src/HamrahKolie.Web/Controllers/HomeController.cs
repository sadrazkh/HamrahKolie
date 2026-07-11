using System.Diagnostics;
using HamrahKolie.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Controllers;

/// <summary>
/// صفحات عمومی سایت. در نسخه اول محتوای پایه ارائه می‌شود؛ در مراحل بعد این صفحات
/// از سیستم مدیریت محتوا و صفحه‌ساز تغذیه خواهند شد.
/// </summary>
public class HomeController : Controller
{
    [OutputCache(Duration = 60)]
    public IActionResult Index() => View();

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
