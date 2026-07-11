using System.Text.Json;
using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Application.PageBuilder;

/// <summary>یک آیتم آمار (برای سکشن Stats).</summary>
public record StatItem(string Value = "", string Label = "");

/// <summary>یک کارت (برای سکشن‌های FeatureCards و Steps).</summary>
public record CardItem(string Title = "", string Text = "");

/// <summary>کمک‌کننده برای خواندن آیتم‌های نوع‌دار از SettingsJson.</summary>
public static class SectionSettings
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<StatItem> GetStats(this PageSection s) => Parse<StatItem>(s.SettingsJson, "stats");
    public static IReadOnlyList<CardItem> GetCards(this PageSection s) => Parse<CardItem>(s.SettingsJson, "cards");

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
