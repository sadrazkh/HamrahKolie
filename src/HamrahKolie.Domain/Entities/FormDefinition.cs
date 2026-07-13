using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>تعریف یک فرم در فرم‌ساز.</summary>
public class FormDefinition : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string? SuccessMessage { get; set; }
    public string SubmitLabel { get; set; } = "ارسال";
    public bool IsEnabled { get; set; } = true;

    /// <summary>در صورت true، هنگام ثبت پاسخ به کارکنان اطلاع داده می‌شود.</summary>
    public bool NotifyStaff { get; set; } = true;

    public ICollection<FormField> Fields { get; set; } = new List<FormField>();
}

/// <summary>یک فیلد از فرم.</summary>
public class FormField : BaseEntity
{
    public long FormDefinitionId { get; set; }
    public FormDefinition Form { get; set; } = default!;

    public string Label { get; set; } = default!;
    /// <summary>نام فنی فیلد (کلید در JSON پاسخ).</summary>
    public string Name { get; set; } = default!;
    public FormFieldType Type { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    /// <summary>گزینه‌ها برای Select/Radio (با خط جدید یا کاما).</summary>
    public string? Options { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>یک پاسخ ثبت‌شده به فرم.</summary>
public class FormSubmission : BaseEntity
{
    public long FormDefinitionId { get; set; }
    public FormDefinition Form { get; set; } = default!;

    /// <summary>داده پاسخ به‌صورت JSON (نام فیلد → مقدار).</summary>
    public string DataJson { get; set; } = "{}";

    public string? IpAddress { get; set; }
    public bool IsReviewed { get; set; }
    public string? ReviewNote { get; set; }
}
