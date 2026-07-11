using HamrahKolie.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>
/// ذخیره‌سازی فایل روی دیسک محلی زیر wwwroot/uploads.
/// این پیاده‌سازی پشت اینترفیس <see cref="IStorageService"/> است تا بعداً بتوان به S3 مهاجرت کرد.
/// </summary>
public sealed class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private const string RootFolder = "uploads";

    public LocalStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<StoredFile> SaveAsync(
        Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var ext = Path.GetExtension(originalFileName);
        var safeName = $"{Guid.NewGuid():N}{ext}".ToLowerInvariant();

        // ساختار پوشه‌ای بر اساس سال/ماه.
        var relativeDir = Path.Combine(RootFolder, now.ToString("yyyy"), now.ToString("MM"));
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, safeName);
        long size;
        await using (var fs = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write))
        {
            await content.CopyToAsync(fs, ct);
            size = fs.Length;
        }

        var storedPath = Path.Combine(relativeDir, safeName).Replace('\\', '/');
        var url = "/" + storedPath;
        return new StoredFile(storedPath, url, size);
    }

    public Task DeleteAsync(string storedPath, CancellationToken ct = default)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var absolutePath = Path.Combine(webRoot, storedPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);
        return Task.CompletedTask;
    }
}
