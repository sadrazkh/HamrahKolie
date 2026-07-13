using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Forms;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.FormManage)]
public class FormsController : Controller
{
    private const int PageSize = 25;
    private readonly IFormService _forms;

    public FormsController(IFormService forms) => _forms = forms;

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "فرم‌ها";
        return View(await _forms.GetAllAsync());
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "فرم جدید";
        return View("EditForm", new FormInput());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FormInput input)
    {
        if (!ModelState.IsValid) return View("EditForm", input);
        var id = await _forms.CreateFormAsync(input);
        TempData["Success"] = "فرم ساخته شد. اکنون فیلدها را اضافه کنید.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var form = await _forms.GetWithFieldsAsync(id);
        if (form is null) return NotFound();
        ViewData["Title"] = "ویرایش فرم";
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateForm(FormInput input)
    {
        await _forms.UpdateFormAsync(input);
        TempData["Success"] = "تنظیمات فرم ذخیره شد.";
        return RedirectToAction(nameof(Edit), new { id = input.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddField(long formId, FormFieldInput input)
    {
        var ok = await _forms.AddFieldAsync(formId, input);
        TempData[ok ? "Success" : "Error"] = ok ? "فیلد افزوده شد." : "افزودن فیلد ناموفق بود.";
        return RedirectToAction(nameof(Edit), new { id = formId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteField(long fieldId, long formId)
    {
        await _forms.DeleteFieldAsync(fieldId);
        TempData["Success"] = "فیلد حذف شد.";
        return RedirectToAction(nameof(Edit), new { id = formId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        await _forms.DeleteFormAsync(id);
        TempData["Success"] = "فرم حذف شد.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Submissions(long id, int page = 1)
    {
        var form = await _forms.GetWithFieldsAsync(id);
        if (form is null) return NotFound();
        ViewData["Title"] = $"پاسخ‌های «{form.Title}»";
        ViewData["Form"] = form;
        var result = await _forms.GetSubmissionsAsync(id, page, PageSize);
        return View(result);
    }

    public async Task<IActionResult> Submission(long id)
    {
        var submission = await _forms.GetSubmissionAsync(id);
        if (submission is null) return NotFound();
        ViewData["Title"] = "پاسخ فرم";
        return View(submission);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkReviewed(long id)
    {
        await _forms.MarkReviewedAsync(id);
        TempData["Success"] = "پاسخ بررسی‌شده علامت خورد.";
        return RedirectToAction(nameof(Submission), new { id });
    }
}
