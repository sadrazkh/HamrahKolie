using HamrahKolie.Application.Common.Interfaces;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>منبع زمان واقعی سیستم (UTC).</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
