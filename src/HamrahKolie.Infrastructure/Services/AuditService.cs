using System.Text.Json;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Infrastructure.Persistence;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>ثبت رویدادهای حساس در جدول AuditLogs (فقط درج).</summary>
public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditService(ApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task LogAsync(
        string action,
        string? description = null,
        string? entityType = null,
        string? entityId = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            OccurredAt = _clock.UtcNow,
            Action = action,
            Description = description,
            EntityType = entityType,
            EntityId = entityId,
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
