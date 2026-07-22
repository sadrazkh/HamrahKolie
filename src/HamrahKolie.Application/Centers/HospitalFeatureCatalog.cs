using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Centers;

/// <summary>
/// کاتالوگ امکانات پورتال مرکز درمانی: منبع واحد برای نمایش در پنل مدیریت و پورتال.
/// افزودن امکان جدید فقط با اضافه‌کردن یک ردیف اینجا و یک عضو در <see cref="HospitalFeature"/> انجام می‌شود.
/// </summary>
public static class HospitalFeatureCatalog
{
    public record FeatureInfo(HospitalFeature Flag, string Title, string Description, string Icon);

    public static readonly IReadOnlyList<FeatureInfo> All = new List<FeatureInfo>
    {
        new(HospitalFeature.PatientRegistration, "ثبت بیمار جدید", "امکان معرفی و ثبت بیمار جدید توسط مرکز.", "＋"),
        new(HospitalFeature.DocumentUpload, "بارگذاری مدارک", "پیوست کردن تصویر یا PDF مدارک بیمار.", "📎"),
        new(HospitalFeature.EditPatient, "ویرایش بیمار", "اصلاح اطلاعات بیمار پس از ثبت اولیه.", "✎"),
        new(HospitalFeature.MessageExperts, "گفتگو با کارشناسان", "ارسال پیام به کارشناسان خیریه دربارهٔ بیمار.", "💬"),
        new(HospitalFeature.ViewStatistics, "داشبورد و آمار", "نمایش آمار بیماران و وضعیت آن‌ها.", "📊"),
        new(HospitalFeature.ExportPatients, "خروجی بیماران", "دریافت فایل CSV از فهرست بیماران مرکز.", "⤓"),
        new(HospitalFeature.ViewSensitive, "اطلاعات حساس", "نمایش کد ملی و اطلاعات حساس بیمار.", "🔒"),
    };

    /// <summary>عنوان فارسی یک امکان.</summary>
    public static string Title(HospitalFeature flag)
        => All.FirstOrDefault(f => f.Flag == flag)?.Title ?? flag.ToString();

    /// <summary>تبدیل bitmask به فهرست امکاناتِ فعال.</summary>
    public static IEnumerable<FeatureInfo> Enabled(HospitalFeature features)
        => All.Where(f => (features & f.Flag) == f.Flag);

    /// <summary>ترکیب فهرست فلگ‌های انتخاب‌شده به یک bitmask.</summary>
    public static HospitalFeature Combine(IEnumerable<HospitalFeature>? flags)
        => flags is null ? HospitalFeature.None : flags.Aggregate(HospitalFeature.None, (acc, f) => acc | f);
}
