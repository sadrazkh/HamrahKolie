namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>
/// سرویس دسترسی به تنظیمات سامانه. مقادیر Cache می‌شوند و پس از تغییر، Cache باطل می‌شود.
/// </summary>
public interface ISettingService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task<string> GetOrDefaultAsync(string key, string defaultValue, CancellationToken ct = default);
    Task SetAsync(string key, string? value, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string?>> GetGroupAsync(string group, CancellationToken ct = default);
    void InvalidateCache();
}
