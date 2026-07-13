using HamrahKolie.Application.Common.Models;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Application.Forms;

public record FormSubmitResult(bool Success, string? SuccessMessage, IReadOnlyDictionary<string, string>? Errors);

/// <summary>ورودی افزودن/ویرایش یک فیلد فرم.</summary>
public class FormFieldInput
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Name { get; set; }
    public FormFieldType Type { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>ورودی ایجاد/ویرایش یک فرم.</summary>
public class FormInput
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? SuccessMessage { get; set; }
    public string SubmitLabel { get; set; } = "ارسال";
    public bool IsEnabled { get; set; } = true;
    public bool NotifyStaff { get; set; } = true;
}

public interface IFormService
{
    // عمومی
    Task<FormDefinition?> GetEnabledBySlugAsync(string slug, CancellationToken ct = default);
    Task<FormSubmitResult> SubmitAsync(string slug, IReadOnlyDictionary<string, string> values, string? ip, CancellationToken ct = default);

    // مدیریت — فرم‌ها
    Task<IReadOnlyList<FormDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<FormDefinition?> GetWithFieldsAsync(long id, CancellationToken ct = default);
    Task<long> CreateFormAsync(FormInput input, CancellationToken ct = default);
    Task<bool> UpdateFormAsync(FormInput input, CancellationToken ct = default);
    Task<bool> DeleteFormAsync(long id, CancellationToken ct = default);

    // مدیریت — فیلدها
    Task<bool> AddFieldAsync(long formId, FormFieldInput input, CancellationToken ct = default);
    Task<bool> DeleteFieldAsync(long fieldId, CancellationToken ct = default);

    // مدیریت — پاسخ‌ها
    Task<PagedResult<FormSubmission>> GetSubmissionsAsync(long formId, int page, int pageSize, CancellationToken ct = default);
    Task<FormSubmission?> GetSubmissionAsync(long submissionId, CancellationToken ct = default);
    Task<int> GetNewSubmissionCountAsync(CancellationToken ct = default);
    Task<bool> MarkReviewedAsync(long submissionId, CancellationToken ct = default);
}
