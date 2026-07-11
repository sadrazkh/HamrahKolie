using HamrahKolie.Application.Authorization;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using HamrahKolie.Web.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Web.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission(Permissions.MediaView)]
public class MediaController : Controller
{
    private const long MaxBytes = 10 * 1024 * 1024; // ۱۰ مگابایت
    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml", "application/pdf" };

    private readonly ApplicationDbContext _db;
    private readonly IStorageService _storage;
    private readonly IAuditService _audit;

    public MediaController(ApplicationDbContext db, IStorageService storage, IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "کتابخانه رسانه";
        var files = await _db.MediaFiles.AsNoTracking()
            .OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        return View(files);
    }

    [HttpPost]
    [HasPermission(Permissions.MediaUpload)]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxBytes + 1024)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "فایلی انتخاب نشده است.";
            return RedirectToAction(nameof(Index));
        }
        if (file.Length > MaxBytes)
        {
            TempData["Error"] = "حجم فایل نباید بیش از ۱۰ مگابایت باشد.";
            return RedirectToAction(nameof(Index));
        }
        if (!AllowedTypes.Contains(file.ContentType))
        {
            TempData["Error"] = "نوع فایل مجاز نیست. تصویر یا PDF بارگذاری کنید.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(stream, file.FileName, file.ContentType);

        var media = new MediaFile
        {
            FileName = Path.GetFileName(file.FileName),
            StoredPath = stored.StoredPath,
            Url = stored.Url,
            ContentType = file.ContentType,
            SizeBytes = stored.SizeBytes,
        };
        _db.MediaFiles.Add(media);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Media.Upload", $"فایل «{media.FileName}» بارگذاری شد.", "Media", media.Id.ToString());

        TempData["Success"] = "فایل با موفقیت بارگذاری شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [HasPermission(Permissions.MediaDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var media = await _db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id);
        if (media is null) return NotFound();

        await _storage.DeleteAsync(media.StoredPath);
        _db.MediaFiles.Remove(media); // حذف نرم رکورد
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Media.Delete", $"فایل «{media.FileName}» حذف شد.", "Media", id.ToString());

        TempData["Success"] = "فایل حذف شد.";
        return RedirectToAction(nameof(Index));
    }
}
