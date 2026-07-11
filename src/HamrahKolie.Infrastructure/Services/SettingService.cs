using System.Text.Json;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>پیاده‌سازی سرویس تنظیمات با Cache حافظه‌ای.</summary>
public sealed class SettingService : ISettingService
{
    private const string CacheKey = "settings:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public SettingService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    private async Task<Dictionary<string, Setting>> LoadAsync(CancellationToken ct)
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var list = await _db.Settings.AsNoTracking().ToListAsync(ct);
            return list.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        }))!;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.TryGetValue(key, out var s) ? s.Value : null;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var raw = await GetAsync(key, ct);
        if (string.IsNullOrEmpty(raw)) return default;

        if (typeof(T) == typeof(string)) return (T)(object)raw;
        try
        {
            if (typeof(T).IsPrimitive || typeof(T) == typeof(decimal) || typeof(T) == typeof(bool))
                return (T)Convert.ChangeType(raw, typeof(T));
            return JsonSerializer.Deserialize<T>(raw);
        }
        catch
        {
            return default;
        }
    }

    public async Task<string> GetOrDefaultAsync(string key, string defaultValue, CancellationToken ct = default)
        => await GetAsync(key, ct) ?? defaultValue;

    public async Task SetAsync(string key, string? value, CancellationToken ct = default)
    {
        var entity = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (entity is null)
        {
            entity = new Setting { Key = key, Value = value };
            _db.Settings.Add(entity);
        }
        else
        {
            entity.Value = value;
        }
        await _db.SaveChangesAsync(ct);
        InvalidateCache();
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(string group, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct);
        return all.Values
            .Where(s => string.Equals(s.Group, group, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase);
    }

    public void InvalidateCache() => _cache.Remove(CacheKey);
}
