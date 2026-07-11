namespace HamrahKolie.Domain.Enums;

/// <summary>نوع بلاک (سکشن) در صفحه‌ساز. هر نوع، رندر و فیلدهای مخصوص خود را دارد.</summary>
public enum SectionType
{
    /// <summary>بخش قهرمان (Hero) با عنوان بزرگ، توضیح و دکمه‌ها.</summary>
    Hero = 0,
    /// <summary>آمارهای مؤسسه (شمارنده).</summary>
    Stats = 1,
    /// <summary>کارت‌های ویژگی/مشکلات (عنوان + متن).</summary>
    FeatureCards = 2,
    /// <summary>مراحل شماره‌دار.</summary>
    Steps = 3,
    /// <summary>آخرین اخبار و مقالات (از پایگاه داده).</summary>
    LatestContent = 4,
    /// <summary>متن غنی آزاد.</summary>
    RichText = 5,
    /// <summary>فراخوان به اقدام (بنر با دکمه).</summary>
    CallToAction = 6,
}

/// <summary>پس‌زمینه سکشن.</summary>
public enum SectionBackground
{
    Default = 0,
    Surface = 1,
    Tint = 2,
    Dark = 3,
}

/// <summary>فاصله عمودی سکشن.</summary>
public enum SectionPadding
{
    Normal = 0,
    Compact = 1,
    Spacious = 2,
}
