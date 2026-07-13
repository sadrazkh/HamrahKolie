using System.ComponentModel.DataAnnotations;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Centers;

/// <summary>ورودی ثبت/ویرایش مرکز دیالیز.</summary>
public class CenterInput
{
    public long Id { get; set; }

    [Required(ErrorMessage = "نام مرکز را وارد کنید.")]
    [StringLength(250)]
    public string Name { get; set; } = string.Empty;

    public CenterType Type { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Phone { get; set; }
    public string? WorkingHours { get; set; }
    public string? Services { get; set; }
    public string? Facilities { get; set; }
    public string? DialysisTypes { get; set; }
    public string? AccessibilityNotes { get; set; }
    public string? Website { get; set; }
}

public interface ICenterService
{
    // عمومی
    Task<PagedResult<DialysisCenter>> GetApprovedListAsync(string? province, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<DialysisCenter?> GetApprovedBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetProvincesAsync(CancellationToken ct = default);
    Task<long> SubmitPublicAsync(CenterInput input, CancellationToken ct = default);

    // مدیریت
    Task<PagedResult<DialysisCenter>> GetAdminListAsync(bool? approved, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<DialysisCenter?> GetAsync(long id, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(CancellationToken ct = default);
    Task<long> CreateAsync(CenterInput input, bool approved, CancellationToken ct = default);
    Task<bool> UpdateAsync(CenterInput input, CancellationToken ct = default);
    Task<bool> SetApprovalAsync(long id, bool approved, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
