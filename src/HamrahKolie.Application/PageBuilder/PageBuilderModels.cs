using System.Text.Json;
using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Application.PageBuilder;

/// <summary>یک آیتم آمار (برای سکشن Stats).</summary>
public record StatItem(string Value = "", string Label = "");

/// <summary>یک کارت (برای سکشن‌های FeatureCards و Steps).</summary>
public record CardItem(string Title = "", string Text = "");

/// <summary>
/// تنظیمات نمایشی هر سکشن. داخل SettingsJson ذخیره می‌شود تا بدون تغییر مکرر
/// ساختار دیتابیس، کنترل‌های ظاهری صفحه‌ساز قابل توسعه باشند.
/// </summary>
public sealed record SectionStyle(
    string? BackgroundColor = null,
    string? TextColor = null,
    string? AccentColor = null,
    string TextAlign = "start",
    int ContentWidth = 1180,
    int MinHeight = 0,
    int PaddingTop = 64,
    int PaddingBottom = 64,
    int PaddingInline = 16,
    int MarginTop = 0,
    int MarginBottom = 0,
    int BorderRadius = 0,
    string Shadow = "none",
    string? CssClass = null,
    string BackgroundPosition = "center",
    int OverlayOpacity = 0,
    string Animation = "none");

/// <summary>کمک‌کننده برای خواندن آیتم‌های نوع‌دار از SettingsJson.</summary>
public static class SectionSettings
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<StatItem> GetStats(this PageSection s) => Parse<StatItem>(s.SettingsJson, "stats");
    public static IReadOnlyList<CardItem> GetCards(this PageSection s) => Parse<CardItem>(s.SettingsJson, "cards");

    public static SectionStyle GetStyle(this PageSection s)
    {
        var root = ParseRoot(s.SettingsJson);
        if (root.HasValue && root.Value.TryGetProperty("style", out var style))
        {
            try { return style.Deserialize<SectionStyle>(Options) ?? new SectionStyle(); }
            catch { }
        }
        return new SectionStyle();
    }

    /// <summary>بعد از نخستین تغییر چیدمان، ترتیب ذخیره‌شده باید روی قالب ویژهٔ صفحه نیز اعمال شود.</summary>
    public static bool UsesBuilderOrder(this PageSection s)
    {
        var root = ParseRoot(s.SettingsJson);
        return root.HasValue
            && root.Value.TryGetProperty("builderOrder", out var value)
            && value.ValueKind is JsonValueKind.True;
    }

    public static bool HasStyleSettings(this PageSection s)
    {
        var root = ParseRoot(s.SettingsJson);
        return root.HasValue
            && root.Value.TryGetProperty("style", out var value)
            && value.ValueKind is JsonValueKind.Object;
    }

    /// <summary>تعداد آیتم‌های نمایش‌داده‌شده (برای LatestContent).</summary>
    public static int GetCount(this PageSection s, int fallback = 3)
    {
        var root = ParseRoot(s.SettingsJson);
        if (root.HasValue && root.Value.TryGetProperty("count", out var c) && c.TryGetInt32(out var n) && n > 0)
            return n;
        return fallback;
    }

    private static IReadOnlyList<T> Parse<T>(string? json, string property)
    {
        var root = ParseRoot(json);
        if (root is null || !root.Value.TryGetProperty(property, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<T>();
        try
        {
            return arr.Deserialize<List<T>>(Options) ?? new List<T>();
        }
        catch
        {
            return Array.Empty<T>();
        }
    }

    private static JsonElement? ParseRoot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}
