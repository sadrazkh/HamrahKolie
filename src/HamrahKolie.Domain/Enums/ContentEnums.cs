namespace HamrahKolie.Domain.Enums;

/// <summary>نوع محتوا. یک مدل یکپارچه برای انواع محتوای سایت.</summary>
public enum ContentType
{
    /// <summary>صفحه ثابت (درباره ما، تماس، ...).</summary>
    Page = 0,
    /// <summary>خبر.</summary>
    News = 1,
    /// <summary>مقاله آموزشی.</summary>
    Article = 2,
    /// <summary>داستان بیمار / روایت امید.</summary>
    PatientStory = 3,
}

/// <summary>وضعیت انتشار محتوا.</summary>
public enum ContentStatus
{
    /// <summary>پیش‌نویس.</summary>
    Draft = 0,
    /// <summary>در انتظار بازبینی.</summary>
    Review = 1,
    /// <summary>زمان‌بندی‌شده برای انتشار.</summary>
    Scheduled = 2,
    /// <summary>منتشرشده.</summary>
    Published = 3,
    /// <summary>بایگانی‌شده.</summary>
    Archived = 4,
}

/// <summary>محل نمایش منو.</summary>
public enum MenuLocation
{
    Header = 0,
    Footer = 1,
}
