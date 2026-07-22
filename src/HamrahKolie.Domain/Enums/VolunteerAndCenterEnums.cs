namespace HamrahKolie.Domain.Enums;

/// <summary>وضعیت داوطلب.</summary>
public enum VolunteerStatus
{
    Pending = 0,     // در انتظار بررسی
    Approved = 1,    // تأییدشده
    Active = 2,      // فعال
    Inactive = 3,    // غیرفعال
    Blacklisted = 4, // لیست سیاه
}

/// <summary>نوع همکاری داوطلبانه.</summary>
public enum CollaborationType
{
    Medical = 0,          // پزشکی
    Nursing = 1,          // پرستاری
    SocialWork = 2,       // مددکاری
    Psychology = 3,       // روان‌شناسی
    Transport = 4,        // حمل‌ونقل
    ContentCreation = 5,  // تولید محتوا
    Design = 6,           // طراحی
    Technology = 7,       // فناوری
    Photography = 8,      // عکاسی
    EventOrganizing = 9,  // برگزاری رویداد
    Fundraising = 10,     // جمع‌آوری کمک
    Organizational = 11,  // همکاری سازمانی
    Other = 12,
}

/// <summary>نوع مرکز دیالیز.</summary>
public enum CenterType
{
    Governmental = 0, // دولتی
    Private = 1,      // خصوصی
    Charity = 2,      // خیریه
    University = 3,   // دانشگاهی
    Other = 4,
}

/// <summary>
/// امکانات پورتال مرکز درمانی. هر مرکز می‌تواند مجموعه‌ای دلخواه از این امکانات را
/// (که توسط مدیر سامانه تعیین می‌شود) در پورتال خود داشته باشد. به‌صورت bitmask ذخیره می‌شود.
/// </summary>
[Flags]
public enum HospitalFeature
{
    None = 0,

    /// <summary>ثبت بیمار جدید توسط مرکز.</summary>
    PatientRegistration = 1 << 0,

    /// <summary>بارگذاری مدارک برای بیماران.</summary>
    DocumentUpload = 1 << 1,

    /// <summary>ویرایش اطلاعات بیمار پس از ثبت.</summary>
    EditPatient = 1 << 2,

    /// <summary>گفتگو/ارسال پیام به کارشناسان خیریه.</summary>
    MessageExperts = 1 << 3,

    /// <summary>مشاهده داشبورد و آمار بیماران مرکز.</summary>
    ViewStatistics = 1 << 4,

    /// <summary>خروجی گرفتن (CSV) از فهرست بیماران.</summary>
    ExportPatients = 1 << 5,

    /// <summary>مشاهده اطلاعات حساس بیمار (کد ملی و…).</summary>
    ViewSensitive = 1 << 6,

    /// <summary>مجموعهٔ پیش‌فرض برای مراکز جدید.</summary>
    Default = PatientRegistration | DocumentUpload | ViewStatistics,
}
