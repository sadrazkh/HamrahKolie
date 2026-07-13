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
