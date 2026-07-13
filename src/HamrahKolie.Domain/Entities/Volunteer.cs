using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک داوطلب همکاری.</summary>
public class Volunteer : BaseEntity
{
    public string FullName { get; set; } = default!;
    public string Mobile { get; set; } = default!;
    public string? Email { get; set; }

    public string? Province { get; set; }
    public string? City { get; set; }

    public CollaborationType CollaborationType { get; set; }

    /// <summary>مهارت‌ها (متن آزاد یا با کاما).</summary>
    public string? Skills { get; set; }

    /// <summary>زمان‌های در دسترس.</summary>
    public string? AvailableTimes { get; set; }

    /// <summary>سوابق و توضیحات.</summary>
    public string? Background { get; set; }

    public VolunteerStatus Status { get; set; } = VolunteerStatus.Pending;

    /// <summary>یادداشت داخلی مدیریت.</summary>
    public string? AdminNotes { get; set; }

    public bool ConsentAccepted { get; set; }
}
