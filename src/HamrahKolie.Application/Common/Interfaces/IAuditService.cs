namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>ثبت رویدادهای حساس در Audit Log.</summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        string? description = null,
        string? entityType = null,
        string? entityId = null,
        object? metadata = null,
        CancellationToken ct = default);
}
