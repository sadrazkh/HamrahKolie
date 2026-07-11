using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Web.Infrastructure.Vite;

/// <summary>
/// خروجی Build فرانت‌اند (Vite) را از روی manifest.json می‌خواند تا Razor بتواند
/// فایل‌های نهایی JS/CSS جزیره‌های Vue را با نام هش‌دار درست صدا بزند.
/// اگر خروجی Build وجود نداشته باشد، به‌جای خطا، خالی برمی‌گرداند تا اپلیکیشن
/// حتی بدون اجرای «npm run build» بالا بیاید.
/// </summary>
public sealed class ViteManifestService
{
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;
    private const string ManifestPath = "dist/.vite/manifest.json";
    private const string PublicBase = "/dist/";

    public ViteManifestService(IWebHostEnvironment env, IMemoryCache cache)
    {
        _env = env;
        _cache = cache;
    }

    public bool IsBuilt => LoadManifest() is not null;

    private Dictionary<string, ManifestChunk>? LoadManifest()
    {
        return _cache.GetOrCreate("vite:manifest", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _env.IsDevelopment()
                ? TimeSpan.FromSeconds(2) : TimeSpan.FromHours(12);

            var full = Path.Combine(_env.WebRootPath, ManifestPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) return null;

            var json = File.ReadAllText(full);
            return JsonSerializer.Deserialize<Dictionary<string, ManifestChunk>>(json);
        });
    }

    /// <summary>مسیر عمومی فایل JS اصلی یک entry (مثل «src/islands/main.ts»).</summary>
    public string? GetScriptPath(string entry)
    {
        var manifest = LoadManifest();
        if (manifest is null || !manifest.TryGetValue(entry, out var chunk)) return null;
        return PublicBase + chunk.File;
    }

    /// <summary>مسیرهای عمومی فایل‌های CSS مرتبط با یک entry.</summary>
    public IReadOnlyList<string> GetStylePaths(string entry)
    {
        var manifest = LoadManifest();
        if (manifest is null || !manifest.TryGetValue(entry, out var chunk) || chunk.Css is null)
            return Array.Empty<string>();
        return chunk.Css.Select(c => PublicBase + c).ToList();
    }

    private sealed class ManifestChunk
    {
        [System.Text.Json.Serialization.JsonPropertyName("file")]
        public string File { get; set; } = default!;

        [System.Text.Json.Serialization.JsonPropertyName("css")]
        public string[]? Css { get; set; }
    }
}
