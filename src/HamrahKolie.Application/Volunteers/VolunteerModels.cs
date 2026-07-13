using System.ComponentModel.DataAnnotations;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Volunteers;

/// <summary>ورودی فرم ثبت‌نام داوطلب.</summary>
public class VolunteerInput
{
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید با ۰۹ شروع شود و ۱۱ رقم باشد.")]
    public string Mobile { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "ایمیل نامعتبر است.")]
    public string? Email { get; set; }

    public string? Province { get; set; }
    public string? City { get; set; }
    public CollaborationType CollaborationType { get; set; }
    public string? Skills { get; set; }
    public string? AvailableTimes { get; set; }

    [StringLength(1500)]
    public string? Background { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "پذیرش قوانین همکاری الزامی است.")]
    public bool ConsentAccepted { get; set; }
}

public interface IVolunteerService
{
    Task<long> SubmitAsync(VolunteerInput input, CancellationToken ct = default);

    Task<PagedResult<Volunteer>> GetAdminListAsync(
        VolunteerStatus? status, CollaborationType? type, string? search, int page, int pageSize, CancellationToken ct = default);

    Task<Volunteer?> GetAsync(long id, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
    Task<bool> SetStatusAsync(long id, VolunteerStatus status, CancellationToken ct = default);
    Task<bool> SetNotesAsync(long id, string? notes, CancellationToken ct = default);
}
