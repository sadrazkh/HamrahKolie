using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Web.Helpers;

/// <summary>نام‌های فارسی وضعیت و اولویت درخواست حمایت برای نمایش.</summary>
public static class SupportRequestDisplay
{
    public static string Status(SupportRequestStatus s) => s switch
    {
        SupportRequestStatus.Submitted => "ثبت اولیه",
        SupportRequestStatus.PendingReview => "در انتظار بررسی",
        SupportRequestStatus.NeedsDocuments => "نیازمند تکمیل مدارک",
        SupportRequestStatus.SocialWorkerReview => "بررسی مددکار",
        SupportRequestStatus.MedicalReview => "بررسی پزشکی",
        SupportRequestStatus.PreliminaryApproved => "تأیید اولیه",
        SupportRequestStatus.Rejected => "ردشده",
        SupportRequestStatus.FinalApproved => "تأیید نهایی",
        SupportRequestStatus.SupportAssigned => "تخصیص حمایت",
        SupportRequestStatus.InProgress => "در حال اجرا",
        SupportRequestStatus.Completed => "تکمیل‌شده",
        SupportRequestStatus.Archived => "بایگانی",
        _ => s.ToString()
    };

    public static string Priority(RequestPriority p) => p switch
    {
        RequestPriority.Low => "کم",
        RequestPriority.Normal => "عادی",
        RequestPriority.High => "زیاد",
        RequestPriority.Urgent => "فوری",
        _ => p.ToString()
    };

    public static string PriorityBadge(RequestPriority p) => p switch
    {
        RequestPriority.Urgent => "badge-danger",
        RequestPriority.High => "badge-danger",
        _ => "badge"
    };

    public static string Dialysis(DialysisType d) => d switch
    {
        DialysisType.Hemodialysis => "همودیالیز",
        DialysisType.Peritoneal => "دیالیز صفاقی",
        DialysisType.Other => "سایر",
        _ => "نامشخص"
    };

    public static string MaskNationalId(string? id)
        => string.IsNullOrEmpty(id) || id.Length < 4 ? "—" : new string('*', id.Length - 4) + id[^4..];

    public static string MaskMobile(string? m)
        => string.IsNullOrEmpty(m) || m.Length < 11 ? "—" : $"{m[..4]}***{m[^2..]}";
}
