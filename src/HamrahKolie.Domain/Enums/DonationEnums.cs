namespace HamrahKolie.Domain.Enums;

/// <summary>وضعیت کمپین.</summary>
public enum CampaignStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Successful = 4,
    Closed = 5,
}

/// <summary>نوع کمک مالی.</summary>
public enum DonationType
{
    /// <summary>کمک آزاد.</summary>
    General = 0,
    /// <summary>کمک به کمپین مشخص.</summary>
    Campaign = 1,
    /// <summary>کمک ماهانه/مستمر.</summary>
    Monthly = 2,
    /// <summary>نذر.</summary>
    Vow = 3,
    /// <summary>صدقه.</summary>
    Charity = 4,
    /// <summary>کفاره.</summary>
    Kaffara = 5,
    /// <summary>یادبود.</summary>
    Memorial = 6,
    /// <summary>مناسبتی.</summary>
    Occasion = 7,
    /// <summary>حمایت سازمانی.</summary>
    Organizational = 8,
}

/// <summary>روش پرداخت.</summary>
public enum PaymentMethod
{
    Online = 0,
    Offline = 1,
}

/// <summary>وضعیت پرداخت/کمک.</summary>
public enum PaymentStatus
{
    /// <summary>در انتظار پرداخت.</summary>
    Pending = 0,
    /// <summary>موفق و تأییدشده.</summary>
    Succeeded = 1,
    /// <summary>ناموفق.</summary>
    Failed = 2,
    /// <summary>لغوشده توسط کاربر.</summary>
    Canceled = 3,
    /// <summary>بازپرداخت‌شده.</summary>
    Refunded = 4,
}

/// <summary>وضعیت بررسی پرداخت آفلاین.</summary>
public enum OfflineReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
