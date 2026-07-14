namespace HamrahKolie.Application.Authorization;

/// <summary>
/// نقش‌های از پیش تعریف‌شده سامانه به همراه دسترسی‌های پیش‌فرض هر نقش.
/// در هنگام راه‌اندازی Seed می‌شوند. Super Admin به‌صورت ویژه همه دسترسی‌ها را دارد.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrgManager = "OrgManager";
    public const string ContentManager = "ContentManager";
    public const string Author = "Author";
    public const string Editor = "Editor";
    public const string FinanceManager = "FinanceManager";
    public const string Accountant = "Accountant";
    public const string CampaignManager = "CampaignManager";
    public const string SocialWorker = "SocialWorker";
    public const string MedicalExpert = "MedicalExpert";
    public const string RequestOperator = "RequestOperator";
    public const string VolunteerManager = "VolunteerManager";
    public const string Support = "Support";
    public const string ReportViewer = "ReportViewer";
    public const string MedicalCenter = "MedicalCenter";
    public const string PublicUser = "PublicUser";

    public record RoleDefinition(string Name, string DisplayName, string Description, string[] Permissions);

    /// <summary>نشانگر «همه دسترسی‌ها» برای Super Admin.</summary>
    public static readonly string[] AllPermissionsMarker = ["*"];

    public static readonly IReadOnlyList<RoleDefinition> All = new List<RoleDefinition>
    {
        new(SuperAdmin, "مدیر ارشد", "دسترسی کامل به تمام بخش‌ها", AllPermissionsMarker),

        new(OrgManager, "مدیر مؤسسه", "مدیریت کلان مؤسسه و گزارش‌ها", new[]
        {
            Permissions.DashboardView, Permissions.ContentView, Permissions.CampaignView,
            Permissions.DonationView, Permissions.DonationViewFinancial, Permissions.SupportRequestView,
            Permissions.VolunteerView, Permissions.CenterView, Permissions.ReportView,
            Permissions.MessageView, Permissions.UserView, Permissions.AuditLogView,
        }),

        new(ContentManager, "مدیر محتوا", "مدیریت کامل محتوا، رسانه و ساختار سایت", new[]
        {
            Permissions.DashboardView, Permissions.ContentView, Permissions.ContentCreate,
            Permissions.ContentEdit, Permissions.ContentDelete, Permissions.ContentPublish,
            Permissions.MediaView, Permissions.MediaUpload, Permissions.MediaDelete,
            Permissions.MenuManage, Permissions.PageBuilderManage, Permissions.SeoManage,
        }),

        new(Author, "نویسنده", "ایجاد و ویرایش محتوای خود بدون انتشار", new[]
        {
            Permissions.DashboardView, Permissions.ContentView, Permissions.ContentCreate,
            Permissions.ContentEdit, Permissions.MediaView, Permissions.MediaUpload,
        }),

        new(Editor, "ویراستار", "ویرایش و انتشار محتوا", new[]
        {
            Permissions.DashboardView, Permissions.ContentView, Permissions.ContentEdit,
            Permissions.ContentPublish, Permissions.MediaView, Permissions.MediaUpload,
        }),

        new(FinanceManager, "مدیر مالی", "مدیریت کامل مالی و بازپرداخت", new[]
        {
            Permissions.DashboardView, Permissions.DonationView, Permissions.DonationViewFinancial,
            Permissions.DonationRefund, Permissions.DonationVerifyOffline, Permissions.DonationExport,
            Permissions.ReportView,
        }),

        new(Accountant, "حسابدار", "مشاهده و خروجی اطلاعات مالی", new[]
        {
            Permissions.DashboardView, Permissions.DonationView, Permissions.DonationViewFinancial,
            Permissions.DonationExport, Permissions.ReportView,
        }),

        new(CampaignManager, "مدیر کمپین", "مدیریت و انتشار کمپین‌ها", new[]
        {
            Permissions.DashboardView, Permissions.CampaignView, Permissions.CampaignManage,
            Permissions.CampaignPublish, Permissions.DonationView, Permissions.MediaView,
            Permissions.MediaUpload,
        }),

        new(SocialWorker, "مددکار", "بررسی درخواست‌های حمایت", new[]
        {
            Permissions.DashboardView, Permissions.SupportRequestView, Permissions.SupportRequestViewSensitive,
            Permissions.SupportRequestChangeStatus, Permissions.MessageView,
        }),

        new(MedicalExpert, "کارشناس پزشکی", "بررسی پزشکی درخواست‌ها", new[]
        {
            Permissions.DashboardView, Permissions.SupportRequestView, Permissions.SupportRequestViewSensitive,
            Permissions.SupportRequestChangeStatus,
        }),

        new(RequestOperator, "اپراتور درخواست‌ها", "ثبت و ارجاع درخواست‌ها", new[]
        {
            Permissions.DashboardView, Permissions.SupportRequestView, Permissions.SupportRequestAssign,
            Permissions.SupportRequestChangeStatus, Permissions.MessageView,
        }),

        new(VolunteerManager, "مدیر داوطلبان", "مدیریت داوطلبان", new[]
        {
            Permissions.DashboardView, Permissions.VolunteerView, Permissions.VolunteerManage,
            Permissions.MessageView,
        }),

        new(Support, "پشتیبان", "پاسخ به پیام‌ها و پشتیبانی", new[]
        {
            Permissions.DashboardView, Permissions.MessageView, Permissions.CenterView,
        }),

        new(ReportViewer, "مشاهده‌گر گزارش", "مشاهده گزارش‌ها بدون تغییر", new[]
        {
            Permissions.DashboardView, Permissions.ReportView,
        }),

        new(MedicalCenter, "مرکز درمانی", "پورتال ثبت و مدیریت بیماران توسط بیمارستان", new[]
        {
            Permissions.HospitalPortal,
        }),

        new(PublicUser, "کاربر عمومی", "کاربر عادی سایت", Array.Empty<string>()),
    };
}
