using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;

namespace HamrahKolie.Web.Services;

/// <summary>نتیجه آپلود فایل.</summary>
public record UploadResult(MediaFile? Media, string? Error)
{
    public bool Success => Media is not null;
}

/// <summary>آپلود امن فایل و ثبت آن در کتابخانه رسانه (قابل‌استفاده در فرم‌های مختلف).</summary>
public interface IFileUploadService
{
    Task<UploadResult> SaveAsync(IFormFile? file, CancellationToken ct = default);
}

public sealed class FileUploadService : IFileUploadService
{
    // تصویر و PDF تا ۱۰ مگابایت.
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] Allowed =
        { "image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf" };

    private readonly IStorageService _storage;
    private readonly ApplicationDbContext _db;

    public FileUploadService(IStorageService storage, ApplicationDbContext db)
    {
        _storage = storage;
        _db = db;
    }

    public async Task<UploadResult> SaveAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return new UploadResult(null, "فایلی انتخاب نشده است.");
        if (file.Length > MaxBytes)
            return new UploadResult(null, "حجم فایل نباید بیش از ۱۰ مگابایت باشد.");
        if (!Allowed.Contains(file.ContentType))
            return new UploadResult(null, "نوع فایل مجاز نیست. تصویر یا PDF بارگذاری کنید.");

        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(stream, file.FileName, file.ContentType, ct);

        var media = new MediaFile
        {
            FileName = Path.GetFileName(file.FileName),
            StoredPath = stored.StoredPath,
            Url = stored.Url,
            ContentType = file.ContentType,
            SizeBytes = stored.SizeBytes,
        };
        _db.MediaFiles.Add(media);
        await _db.SaveChangesAsync(ct);
        return new UploadResult(media, null);
    }
}
