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

    public async Task<(long Id, string TrackingCode)> SubmitAsync(SupportRequestInput input, string consentVersion, CancellationToken ct = default)
    {
        var request = new SupportRequest
        {
            TrackingCode = await GenerateTrackingAsync(ct),
            Status = SupportRequestStatus.Submitted,
            Priority = RequestPriority.Normal,
            ReferringCenterId = input.ReferringCenterId,
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

        return (request.Id, request.TrackingCode);
    }

    public async Task<bool> AddDocumentAsync(long requestId, long mediaFileId, string title, bool uploadedByApplicant, CancellationToken ct = default)
    {
        if (!await _db.SupportRequests.AnyAsync(r => r.Id == requestId, ct)) return false;
        _db.SupportRequestDocuments.Add(new SupportRequestDocument
        {
            SupportRequestId = requestId,
            MediaFileId = mediaFileId,
            Title = string.IsNullOrWhiteSpace(title) ? "مدرک" : title.Trim(),
            UploadedByApplicant = uploadedByApplicant,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddApplicantDocumentAsync(long requestId, string mobile, long mediaFileId, string title, CancellationToken ct = default)
    {
        var owns = await _db.SupportRequests.AnyAsync(r => r.Id == requestId && r.Mobile == mobile.Trim(), ct);
        if (!owns) return false;
        return await AddDocumentAsync(requestId, mediaFileId, title, uploadedByApplicant: true, ct);
    }

    public async Task<PagedResult<SupportRequest>> GetForCenterAsync(long centerId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.SupportRequests.AsNoTracking().Where(r => r.ReferringCenterId == centerId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<SupportRequest> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<SupportRequest?> GetForCenterDetailAsync(long id, long centerId, CancellationToken ct = default)
        => _db.SupportRequests
            .Include(r => r.Documents).ThenInclude(d => d.MediaFile)
            .Include(r => r.History.OrderBy(h => h.CreatedAt))
            .Include(r => r.Messages.Where(m => m.Visibility == MessageVisibility.Center).OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(r => r.Id == id && r.ReferringCenterId == centerId, ct);

    public async Task<CenterPatientStats> GetCenterStatsAsync(long centerId, CancellationToken ct = default)
    {
        var q = _db.SupportRequests.AsNoTracking().Where(r => r.ReferringCenterId == centerId);
        var byStatus = await q.GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(Func<SupportRequestStatus, bool> pred) => byStatus.Where(x => pred(x.Status)).Sum(x => x.Count);

        var total = byStatus.Sum(x => x.Count);
        var completed = Count(s => s == SupportRequestStatus.Completed);
        var rejected = Count(s => s == SupportRequestStatus.Rejected);
        var open = Count(s => !ClosedStatuses.Contains(s));
        var inReview = Count(s => s is SupportRequestStatus.PendingReview or SupportRequestStatus.NeedsDocuments
            or SupportRequestStatus.SocialWorkerReview or SupportRequestStatus.MedicalReview);
        var thisMonth = await CountCenterThisMonthAsync(centerId, ct);

        return new CenterPatientStats(total, open, inReview, completed, rejected, thisMonth);
    }

    public async Task<IReadOnlyList<SupportRequest>> GetAllForCenterAsync(long centerId, CancellationToken ct = default)
        => await _db.SupportRequests.AsNoTracking()
            .Where(r => r.ReferringCenterId == centerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountCenterThisMonthAsync(long centerId, CancellationToken ct = default)
    {
        var monthStart = new DateTime(_clock.UtcNow.Year, _clock.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.SupportRequests.AsNoTracking()
            .CountAsync(r => r.ReferringCenterId == centerId && r.CreatedAt >= monthStart, ct);
    }

    public async Task<bool> AddCenterMessageAsync(long requestId, long centerId, string authorName, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var request = await _db.SupportRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ReferringCenterId == centerId, ct);
        if (request is null) return false;

        _db.SupportRequestMessages.Add(new SupportRequestMessage
        {
            SupportRequestId = requestId,
            Visibility = MessageVisibility.Center,
            IsFromApplicant = false,
            Body = body.Trim(),
            AuthorName = string.IsNullOrWhiteSpace(authorName) ? "مرکز درمانی" : authorName.Trim(),
        });
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyStaffAsync("پیام جدید از مرکز درمانی",
            $"مرکز «{authorName}» دربارهٔ بیمار {request.TrackingCode} پیام گذاشت.",
            $"/Admin/SupportRequests/Detail/{request.Id}", ct);
        return true;
    }

    public async Task<bool> UpdateForCenterAsync(long id, long centerId, SupportRequestInput input, CancellationToken ct = default)
    {
        var r = await _db.SupportRequests.FirstOrDefaultAsync(x => x.Id == id && x.ReferringCenterId == centerId, ct);
        if (r is null) return false;

        r.ApplicantName = input.ApplicantName.Trim();
        r.Mobile = input.Mobile.Trim();
        r.NationalId = input.NationalId?.Trim();
        r.Province = input.Province?.Trim();
        r.City = input.City?.Trim();
        r.Village = input.Village?.Trim();
        r.ReferredBy = input.ReferredBy?.Trim();
        r.DialysisType = input.DialysisType;
        r.SessionsPerWeek = input.SessionsPerWeek;
        r.NeedType = input.NeedType?.Trim();
        r.EstimatedCost = input.EstimatedCost;
        r.Description = input.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<SupportRequest?> GetForApplicantAsync(string trackingCode, string mobile, CancellationToken ct = default)
    {
        var code = trackingCode.Trim().ToUpperInvariant();
        var m = mobile.Trim();
        return _db.SupportRequests
            .AsNoTracking()
            .Include(r => r.Documents).ThenInclude(d => d.MediaFile)
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
