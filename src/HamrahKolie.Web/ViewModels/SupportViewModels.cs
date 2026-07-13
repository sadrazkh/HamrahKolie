using HamrahKolie.Application.SupportRequests;

namespace HamrahKolie.Web.ViewModels;

public class SupportTrackViewModel
{
    public string TrackingCode { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;

    /// <summary>مرحله: enter = ورود مشخصات، otp = وارد کردن کد.</summary>
    public string Step { get; set; } = "enter";

    /// <summary>کد نمایش‌داده‌شده فقط در محیط توسعه.</summary>
    public string? DevCode { get; set; }

    public string? Error { get; set; }
}

public class SupportViewPageModel
{
    public HamrahKolie.Domain.Entities.SupportRequest Request { get; set; } = default!;
}
