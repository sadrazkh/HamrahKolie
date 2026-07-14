namespace HamrahKolie.Application.Authorization;

/// <summary>
/// فهرست کامل دسترسی‌های سامانه. این کلاس تنها منبع حقیقت (Single Source of Truth)
/// برای دسترسی‌هاست و در هنگام راه‌اندازی در پایگاه داده Seed می‌شود.
/// هر دسترسی یک کلید یکتا، عنوان فارسی و گروه دارد.
/// </summary>
public static class Permissions
{
    public record PermissionDefinition(string Key, string DisplayName, string Group);

    // ── داشبورد و پنل ──────────────────────────────────────────────
    public const string DashboardView = "dashboard.view";

    // ── محتوا (Page/Post/News/Article/...) ─────────────────────────
    public const string ContentView = "content.view";
    public const string ContentCreate = "content.create";
    public const string ContentEdit = "content.edit";
    public const string ContentDelete = "content.delete";
    public const string ContentPublish = "content.publish";

    // ── رسانه ──────────────────────────────────────────────────────
    public const string MediaView = "media.view";
    public const string MediaUpload = "media.upload";
    public const string MediaDelete = "media.delete";

    // ── منو و صفحه‌ساز ──────────────────────────────────────────────
    public const string MenuManage = "menu.manage";
    public const string PageBuilderManage = "pagebuilder.manage";

    // ── کمپین ──────────────────────────────────────────────────────
    public const string CampaignView = "campaign.view";
    public const string CampaignManage = "campaign.manage";
    public const string CampaignPublish = "campaign.publish";

    // ── مالی / کمک‌ها ───────────────────────────────────────────────
    public const string DonationView = "donation.view";
    public const string DonationViewFinancial = "donation.view_financial";
    public const string DonationRefund = "donation.refund";
    public const string DonationVerifyOffline = "donation.verify_offline";
    public const string DonationExport = "donation.export";

    // ── درخواست حمایت ───────────────────────────────────────────────
    public const string SupportRequestView = "support_request.view";
    public const string SupportRequestViewSensitive = "support_request.view_sensitive";
    public const string SupportRequestAssign = "support_request.assign";
    public const string SupportRequestChangeStatus = "support_request.change_status";
    public const string SupportRequestExport = "support_request.export";

    // ── پورتال مرکز درمانی ──────────────────────────────────────────
    public const string HospitalPortal = "hospital.portal";

    // ── داوطلبان ────────────────────────────────────────────────────
    public const string VolunteerView = "volunteer.view";
    public const string VolunteerManage = "volunteer.manage";

    // ── مراکز دیالیز ─────────────────────────────────────────────────
    public const string CenterView = "center.view";
    public const string CenterManage = "center.manage";
    public const string CenterApprove = "center.approve";

    // ── فرم‌ها و پیام‌ها ─────────────────────────────────────────────
    public const string FormManage = "form.manage";
    public const string MessageView = "message.view";

    // ── گزارش‌ها ─────────────────────────────────────────────────────
    public const string ReportView = "report.view";

    // ── کاربران، نقش‌ها و دسترسی‌ها ──────────────────────────────────
    public const string UserView = "user.view";
    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";

    // ── تنظیمات، SEO، سیستم ──────────────────────────────────────────
    public const string SettingsManage = "settings.manage";
    public const string SeoManage = "seo.manage";
    public const string AuditLogView = "audit.view";
    public const string SystemManage = "system.manage"; // Backup، Jobها، Maintenance

    /// <summary>فهرست کامل تعاریف دسترسی برای Seed و ساخت UI مدیریت نقش‌ها.</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All = new List<PermissionDefinition>
    {
        new(DashboardView, "مشاهده داشبورد", "داشبورد"),

        new(ContentView, "مشاهده محتوا", "محتوا"),
        new(ContentCreate, "ایجاد محتوا", "محتوا"),
        new(ContentEdit, "ویرایش محتوا", "محتوا"),
        new(ContentDelete, "حذف محتوا", "محتوا"),
        new(ContentPublish, "انتشار محتوا", "محتوا"),

        new(MediaView, "مشاهده رسانه", "رسانه"),
        new(MediaUpload, "آپلود رسانه", "رسانه"),
        new(MediaDelete, "حذف رسانه", "رسانه"),

        new(MenuManage, "مدیریت منوها", "ساختار سایت"),
        new(PageBuilderManage, "مدیریت صفحه‌ساز", "ساختار سایت"),

        new(CampaignView, "مشاهده کمپین‌ها", "کمپین"),
        new(CampaignManage, "مدیریت کمپین‌ها", "کمپین"),
        new(CampaignPublish, "انتشار کمپین", "کمپین"),

        new(DonationView, "مشاهده کمک‌ها", "مالی"),
        new(DonationViewFinancial, "مشاهده اطلاعات مالی", "مالی"),
        new(DonationRefund, "ثبت بازپرداخت", "مالی"),
        new(DonationVerifyOffline, "تأیید پرداخت آفلاین", "مالی"),
        new(DonationExport, "خروجی گرفتن از کمک‌ها", "مالی"),

        new(SupportRequestView, "مشاهده درخواست‌های حمایت", "درخواست حمایت"),
        new(SupportRequestViewSensitive, "مشاهده اطلاعات حساس بیمار", "درخواست حمایت"),
        new(SupportRequestAssign, "ارجاع درخواست", "درخواست حمایت"),
        new(SupportRequestChangeStatus, "تغییر وضعیت درخواست", "درخواست حمایت"),
        new(SupportRequestExport, "خروجی درخواست‌ها", "درخواست حمایت"),

        new(HospitalPortal, "دسترسی پورتال مرکز درمانی", "مرکز درمانی"),

        new(VolunteerView, "مشاهده داوطلبان", "داوطلبان"),
        new(VolunteerManage, "مدیریت داوطلبان", "داوطلبان"),

        new(CenterView, "مشاهده مراکز دیالیز", "مراکز"),
        new(CenterManage, "مدیریت مراکز دیالیز", "مراکز"),
        new(CenterApprove, "تأیید مراکز دیالیز", "مراکز"),

        new(FormManage, "مدیریت فرم‌ها", "فرم‌ها"),
        new(MessageView, "مشاهده پیام‌ها", "پیام‌ها"),

        new(ReportView, "مشاهده گزارش‌ها", "گزارش‌ها"),

        new(UserView, "مشاهده کاربران", "کاربران"),
        new(UserManage, "مدیریت کاربران", "کاربران"),
        new(RoleManage, "مدیریت نقش‌ها و دسترسی‌ها", "کاربران"),

        new(SettingsManage, "مدیریت تنظیمات", "سیستم"),
        new(SeoManage, "مدیریت سئو", "سیستم"),
        new(AuditLogView, "مشاهده گزارش رویدادها", "سیستم"),
        new(SystemManage, "مدیریت فنی سیستم", "سیستم"),
    };
}
