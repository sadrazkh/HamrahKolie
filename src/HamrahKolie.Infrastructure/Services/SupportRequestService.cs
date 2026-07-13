using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Application.Notifications;
using HamrahKolie.Application.SupportRequests;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class SupportRequestService : ISupportRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    private static readonly SupportRequestStatus[] ClosedStatuses =
        { SupportRequestStatus.Completed, SupportRequestStatus.Archived, SupportRequestStatus.Rejected };

    public SupportRequestService(ApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<string> SubmitAsync(SupportRequestInput input, string consentVersion, CancellationToken ct = default)
    {
        var request = new SupportRequest
        {
            TrackingCode = await GenerateTrackingAsync(ct),
            Status = SupportRequestStatus.Submitted,
            Priority = RequestPriority.Normal,
            ApplicantName = input.ApplicantName.Trim(),
            Mobile = input.Mobile.Trim(),
            NationalId = input.NationalId?.Trim(),
            Province = input.Province?.Trim(),
            City = input.City?.Trim(),
            Village = input.Village?.Trim(),
            TreatmentCenter = input.TreatmentCenter?.Trim(),
            ReferredBy = input.ReferredBy?.Trim(),
            DialysisType = input.DialysisType,
            SessionsPerWeek = input.SessionsPerWeek,
            NeedType = input.NeedType?.Trim(),
            EstimatedCost = input.EstimatedCost,
            InsuranceStatus = input.InsuranceStatus?.Trim(),
            HouseholdSize = input.HouseholdSize,
            Description = input.Description?.Trim(),
            DataProcessingConsent = input.DataProcessingConsent,
            ConsentedAt = _clock.UtcNow,
            ConsentVersion = consentVersion,
            PublicDisclosureConsent = input.PublicDisclosureConsent,
        };
        request.History.Add(new SupportRequestStatusHistory
        {
            FromStatus = null,
            ToStatus = SupportRequestStatus.Submitted,
            Note = "ثبت اولیه درخواست",
        });

        _db.SupportRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyStaffAsync("درخواست حمایت جدید",
            $"درخواست {request.TrackingCode} از {request.Province ?? "—"} ثبت شد.",
            $"/Admin/SupportRequests/Detail/{request.Id}", ct);

        return request.TrackingCode;
    }

    public Task<SupportRequest?> GetForApplicantAsync(string trackingCode, string mobile, CancellationToken ct = default)
    {
        var code = trackingCode.Trim().ToUpperInvariant();
        var m = mobile.Trim();
        return _db.SupportRequests
            .AsNoTracking()
            .Include(r => r.Documents)
            .Include(r => r.History.OrderBy(h => h.CreatedAt))
            .Include(r => r.Messages.Where(msg => msg.Visibility == MessageVisibility.Applicant).OrderBy(msg => msg.CreatedAt))
            .FirstOrDefaultAsync(r => r.TrackingCode == code && r.Mobile == m, ct);
    }

    public async Task<bool> AddApplicantMessageAsync(long requestId, string mobile, string body, CancellationToken ct = default)
    {
        var request = await _db.SupportRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.Mobile == mobile.Trim(), ct);
        if (request is null || string.IsNullOrWhiteSpace(body)) return false;

        _db.SupportRequestMessages.Add(new SupportRequestMessage
        {
            SupportRequestId = request.Id,
            Visibility = MessageVisibility.Applicant,
            IsFromApplicant = true,
            Body = body.Trim(),
            AuthorName = request.ApplicantName,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── مدیریت ───────────────────────────────────────────────────
    public async Task<PagedResult<SupportRequest>> GetAdminListAsync(
        SupportRequestStatus? status, RequestPriority? priority, string? assignedToUserId, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.SupportRequests.AsNoTracking().AsQueryable();
        if (status is not null) q = q.Where(r => r.Status == status);
        if (priority is not null) q = q.Where(r => r.Priority == priority);
        if (!string.IsNullOrWhiteSpace(assignedToUserId)) q = q.Where(r => r.AssignedToUserId == assignedToUserId);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(r => r.TrackingCode.Contains(search) || r.ApplicantName.Contains(search) || r.Mobile.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.Priority).ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<SupportRequest> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<SupportRequest?> GetAdminDetailAsync(long id, CancellationToken ct = default)
        => _db.SupportRequests
            .Include(r => r.Documents).ThenInclude(d => d.MediaFile)
            .Include(r => r.History.OrderBy(h => h.CreatedAt))
            .Include(r => r.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<int> GetOpenCountAsync(CancellationToken ct = default)
        => _db.SupportRequests.CountAsync(r => !ClosedStatuses.Contains(r.Status), ct);

    public async Task<bool> ChangeStatusAsync(long id, SupportRequestStatus newStatus, string? note, CancellationToken ct = default)
    {
        var request = await _db.SupportRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return false;
        if (request.Status == newStatus && string.IsNullOrWhiteSpace(note)) return true;

        var from = request.Status;
        request.Status = newStatus;
        _db.SupportRequestStatusHistory.Add(new SupportRequestStatusHistory
        {
            SupportRequestId = id,
            FromStatus = from,
            ToStatus = newStatus,
            Note = note?.Trim(),
            ChangedByUserId = _currentUser.UserId,
            ChangedByName = _currentUser.UserName,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AssignAsync(long id, string? userId, CancellationToken ct = default)
    {
        var request = await _db.SupportRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return false;
        request.AssignedToUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetPriorityAsync(long id, RequestPriority priority, CancellationToken ct = default)
    {
        var request = await _db.SupportRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return false;
        request.Priority = priority;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddNoteAsync(long id, string body, MessageVisibility visibility, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var exists = await _db.SupportRequests.AnyAsync(r => r.Id == id, ct);
        if (!exists) return false;

        _db.SupportRequestMessages.Add(new SupportRequestMessage
        {
            SupportRequestId = id,
            Visibility = visibility,
            IsFromApplicant = false,
            Body = body.Trim(),
            AuthorUserId = _currentUser.UserId,
            AuthorName = _currentUser.UserName,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<SupportRequest>> FindPossibleDuplicatesAsync(long id, CancellationToken ct = default)
    {
        var request = await _db.SupportRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return Array.Empty<SupportRequest>();

        return await _db.SupportRequests.AsNoTracking()
            .Where(r => r.Id != id &&
                (r.Mobile == request.Mobile ||
                 (request.NationalId != null && r.NationalId == request.NationalId)))
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync(ct);
    }

    private async Task<string> GenerateTrackingAsync(CancellationToken ct)
    {
        while (true)
        {
            var code = "SR" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await _db.SupportRequests.AnyAsync(r => r.TrackingCode == code, ct))
                return code;
        }
    }
}
