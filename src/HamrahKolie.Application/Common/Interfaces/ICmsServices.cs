namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>ساخت نامک (Slug) استاندارد از عنوان، با نرمال‌سازی فارسی.</summary>
public interface ISlugService
{
    /// <summary>یک نامک تمیز از متن ورودی می‌سازد (حروف فارسی/انگلیسی/عدد و خط تیره).</summary>
    string Generate(string input);

    /// <summary>نامک یکتا می‌سازد؛ اگر تکراری بود، پسوند عددی اضافه می‌کند.</summary>
    Task<string> GenerateUniqueAsync(string input, Func<string, Task<bool>> existsAsync, CancellationToken ct = default);
}

/// <summary>پاک‌سازی HTML ورودی کاربر برای جلوگیری از XSS.</summary>
public interface IHtmlSanitizerService
{
    string Sanitize(string? html);
}

/// <summary>ذخیره‌سازی فایل‌ها. پیاده‌سازی محلی در نسخه اول؛ آماده اتصال به S3 در آینده.</summary>
public interface IStorageService
{
    /// <summary>یک فایل ذخیره می‌کند و مسیر ذخیره و نشانی عمومی را برمی‌گرداند.</summary>
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);

    /// <summary>یک فایل را حذف می‌کند.</summary>
    Task DeleteAsync(string storedPath, CancellationToken ct = default);
}

/// <summary>نتیجه ذخیره فایل.</summary>
public record StoredFile(string StoredPath, string Url, long SizeBytes);
