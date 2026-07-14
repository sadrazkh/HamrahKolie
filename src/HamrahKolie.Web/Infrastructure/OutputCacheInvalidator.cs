using HamrahKolie.Application.Common.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Infrastructure;

/// <summary>پیاده‌سازی باطل‌سازی کش خروجی با <see cref="IOutputCacheStore"/>.</summary>
public sealed class OutputCacheInvalidator : IOutputCacheInvalidator
{
    private readonly IOutputCacheStore _store;
    public OutputCacheInvalidator(IOutputCacheStore store) => _store = store;

    public async Task InvalidateAsync(string tag, CancellationToken ct = default)
        => await _store.EvictByTagAsync(tag, ct);
}
