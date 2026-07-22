namespace HamrahKolie.Domain.Enums;

/// <summary>مراحل گردش‌کار درخواست حمایت.</summary>
public enum SupportRequestStatus
{
    /// <summary>ثبت اولیه.</summary>
    Submitted = 0,
    /// <summary>در انتظار بررسی.</summary>
    PendingReview = 1,
    /// <summary>نیازمند تکمیل مدارک.</summary>
    NeedsDocuments = 2,
    /// <summary>بررسی مددکار.</summary>
    SocialWorkerReview = 3,
    /// <summary>بررسی پزشکی.</summary>
    MedicalReview = 4,
    /// <summary>تأیید اولیه.</summary>
    PreliminaryApproved = 5,
    /// <summary>ردشده.</summary>
    Rejected = 6,
    /// <summary>تأیید نهایی.</summary>
    FinalApproved = 7,
    /// <summary>تخصیص حمایت.</summary>
    SupportAssigned = 8,
    /// <summary>در حال اجرا.</summary>
    InProgress = 9,
    /// <summary>تکمیل‌شده.</summary>
    Completed = 10,
    /// <summary>بایگانی.</summary>
    Archived = 11,
}

/// <summary>نوع دیالیز.</summary>
public enum DialysisType
{
    Unknown = 0,
    Hemodialysis = 1,   // همودیالیز
    Peritoneal = 2,     // دیالیز صفاقی
    Other = 3,
}

/// <summary>اولویت درخواست.</summary>
public enum RequestPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3,
}

/// <summary>مخاطب پیام درخواست حمایت.</summary>
public enum MessageVisibility
{
    /// <summary>یادداشت داخلی (فقط کارکنان).</summary>
    Internal = 0,
    /// <summary>پیام قابل مشاهده برای متقاضی.</summary>
    Applicant = 1,
    /// <summary>گفتگوی بین مرکز درمانی معرفی‌کننده و کارشناسان خیریه.</summary>
    Center = 2,
}
