using HamrahKolie.Application.Common.Models;
using HamrahKolie.Application.Notifications;
using HamrahKolie.Application.Volunteers;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class VolunteerService : IVolunteerService
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;
    public VolunteerService(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<long> SubmitAsync(VolunteerInput input, CancellationToken ct = default)
    {
        var v = new Volunteer
        {
            FullName = input.FullName.Trim(),
            Mobile = input.Mobile.Trim(),
            Email = input.Email?.Trim(),
            Province = input.Province?.Trim(),
            City = input.City?.Trim(),
            CollaborationType = input.CollaborationType,
            Skills = input.Skills?.Trim(),
            AvailableTimes = input.AvailableTimes?.Trim(),
            Background = input.Background?.Trim(),
            ConsentAccepted = input.ConsentAccepted,
            Status = VolunteerStatus.Pending,
        };
        _db.Volunteers.Add(v);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyStaffAsync("داوطلب جدید",
            $"{v.FullName} برای همکاری ثبت‌نام کرد.", $"/Admin/Volunteers/Detail/{v.Id}", ct);

        return v.Id;
    }

    public async Task<PagedResult<Volunteer>> GetAdminListAsync(
        VolunteerStatus? status, CollaborationType? type, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.Volunteers.AsNoTracking().AsQueryable();
        if (status is not null) q = q.Where(v => v.Status == status);
        if (type is not null) q = q.Where(v => v.CollaborationType == type);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(v => v.FullName.Contains(search) || v.Mobile.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Volunteer> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<Volunteer?> GetAsync(long id, CancellationToken ct = default)
        => _db.Volunteers.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<int> GetPendingCountAsync(CancellationToken ct = default)
        => _db.Volunteers.CountAsync(v => v.Status == VolunteerStatus.Pending, ct);

    public async Task<bool> SetStatusAsync(long id, VolunteerStatus status, CancellationToken ct = default)
    {
        var v = await _db.Volunteers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return false;
        v.Status = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetNotesAsync(long id, string? notes, CancellationToken ct = default)
    {
        var v = await _db.Volunteers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (v is null) return false;
        v.AdminNotes = notes?.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
