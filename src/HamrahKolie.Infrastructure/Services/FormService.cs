using System.Text.Json;
using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Common.Models;
using HamrahKolie.Application.Forms;
using HamrahKolie.Application.Notifications;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HamrahKolie.Infrastructure.Services;

public sealed class FormService : IFormService
{
    private readonly ApplicationDbContext _db;
    private readonly ISlugService _slug;
    private readonly INotificationService _notifications;

    public FormService(ApplicationDbContext db, ISlugService slug, INotificationService notifications)
    {
        _db = db;
        _slug = slug;
        _notifications = notifications;
    }

    // ── عمومی ────────────────────────────────────────────────────
    public Task<FormDefinition?> GetEnabledBySlugAsync(string slug, CancellationToken ct = default)
        => _db.FormDefinitions.AsNoTracking()
            .Include(f => f.Fields.OrderBy(x => x.SortOrder))
            .FirstOrDefaultAsync(f => f.Slug == slug && f.IsEnabled, ct);

    public async Task<FormSubmitResult> SubmitAsync(
        string slug, IReadOnlyDictionary<string, string> values, string? ip, CancellationToken ct = default)
    {
        var form = await _db.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Slug == slug && f.IsEnabled, ct);
        if (form is null) return new FormSubmitResult(false, null, null);

        var errors = new Dictionary<string, string>();
        var data = new Dictionary<string, string>();

        foreach (var field in form.Fields.OrderBy(f => f.SortOrder))
        {
            values.TryGetValue(field.Name, out var value);
            value = value?.Trim();

            if (field.Type is FormFieldType.Consent)
            {
                if (value is not ("true" or "on" or "True"))
                    errors[field.Name] = "پذیرش الزامی است.";
                data[field.Name] = "پذیرفته شد";
                continue;
            }

            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                errors[field.Name] = "این فیلد الزامی است.";
                continue;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (field.Type == FormFieldType.Mobile && !System.Text.RegularExpressions.Regex.IsMatch(value, @"^09\d{9}$"))
                    errors[field.Name] = "شماره موبایل معتبر نیست.";
                else if (field.Type == FormFieldType.Email && !value.Contains('@'))
                    errors[field.Name] = "ایمیل معتبر نیست.";
            }

            data[field.Name] = value ?? "";
        }

        if (errors.Count > 0)
            return new FormSubmitResult(false, null, errors);

        _db.FormSubmissions.Add(new FormSubmission
        {
            FormDefinitionId = form.Id,
            DataJson = JsonSerializer.Serialize(data),
            IpAddress = ip,
        });
        await _db.SaveChangesAsync(ct);

        if (form.NotifyStaff)
            await _notifications.NotifyStaffAsync($"پاسخ جدید فرم «{form.Title}»", "یک پاسخ جدید ثبت شد.",
                $"/Admin/Forms/Submissions/{form.Id}", ct);

        return new FormSubmitResult(true, form.SuccessMessage ?? "پاسخ شما با موفقیت ثبت شد.", null);
    }

    // ── مدیریت فرم‌ها ────────────────────────────────────────────
    public async Task<IReadOnlyList<FormDefinition>> GetAllAsync(CancellationToken ct = default)
        => await _db.FormDefinitions.AsNoTracking()
            .Select(f => new FormDefinition
            {
                Id = f.Id, Title = f.Title, Slug = f.Slug, IsEnabled = f.IsEnabled,
                Fields = f.Fields, CreatedAt = f.CreatedAt
            })
            .OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public Task<FormDefinition?> GetWithFieldsAsync(long id, CancellationToken ct = default)
        => _db.FormDefinitions.Include(f => f.Fields.OrderBy(x => x.SortOrder)).FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<long> CreateFormAsync(FormInput input, CancellationToken ct = default)
    {
        var form = new FormDefinition();
        await MapFormAsync(form, input, isNew: true, ct);
        _db.FormDefinitions.Add(form);
        await _db.SaveChangesAsync(ct);
        return form.Id;
    }

    public async Task<bool> UpdateFormAsync(FormInput input, CancellationToken ct = default)
    {
        var form = await _db.FormDefinitions.FirstOrDefaultAsync(f => f.Id == input.Id, ct);
        if (form is null) return false;
        await MapFormAsync(form, input, isNew: false, ct);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteFormAsync(long id, CancellationToken ct = default)
    {
        var form = await _db.FormDefinitions.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (form is null) return false;
        _db.FormDefinitions.Remove(form);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── مدیریت فیلدها ────────────────────────────────────────────
    public async Task<bool> AddFieldAsync(long formId, FormFieldInput input, CancellationToken ct = default)
    {
        if (!await _db.FormDefinitions.AnyAsync(f => f.Id == formId, ct)) return false;

        var name = string.IsNullOrWhiteSpace(input.Name) ? _slug.Generate(input.Label).Replace('-', '_') : input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) name = "field_" + Guid.NewGuid().ToString("N")[..6];

        var maxOrder = await _db.FormFields.Where(f => f.FormDefinitionId == formId)
            .Select(f => (int?)f.SortOrder).MaxAsync(ct) ?? 0;

        _db.FormFields.Add(new FormField
        {
            FormDefinitionId = formId,
            Label = input.Label.Trim(),
            Name = name,
            Type = input.Type,
            Required = input.Required,
            Placeholder = input.Placeholder?.Trim(),
            HelpText = input.HelpText?.Trim(),
            Options = input.Options?.Trim(),
            SortOrder = maxOrder + 1,
        });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteFieldAsync(long fieldId, CancellationToken ct = default)
    {
        var field = await _db.FormFields.FirstOrDefaultAsync(f => f.Id == fieldId, ct);
        if (field is null) return false;
        _db.FormFields.Remove(field);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ── پاسخ‌ها ──────────────────────────────────────────────────
    public async Task<PagedResult<FormSubmission>> GetSubmissionsAsync(long formId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        var q = _db.FormSubmissions.AsNoTracking().Where(s => s.FormDefinitionId == formId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<FormSubmission> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public Task<FormSubmission?> GetSubmissionAsync(long submissionId, CancellationToken ct = default)
        => _db.FormSubmissions.Include(s => s.Form).FirstOrDefaultAsync(s => s.Id == submissionId, ct);

    public Task<int> GetNewSubmissionCountAsync(CancellationToken ct = default)
        => _db.FormSubmissions.CountAsync(s => !s.IsReviewed, ct);

    public async Task<bool> MarkReviewedAsync(long submissionId, CancellationToken ct = default)
    {
        var s = await _db.FormSubmissions.FirstOrDefaultAsync(x => x.Id == submissionId, ct);
        if (s is null) return false;
        s.IsReviewed = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task MapFormAsync(FormDefinition form, FormInput input, bool isNew, CancellationToken ct)
    {
        form.Title = input.Title.Trim();
        form.Description = input.Description?.Trim();
        form.SuccessMessage = input.SuccessMessage?.Trim();
        form.SubmitLabel = string.IsNullOrWhiteSpace(input.SubmitLabel) ? "ارسال" : input.SubmitLabel.Trim();
        form.IsEnabled = input.IsEnabled;
        form.NotifyStaff = input.NotifyStaff;

        var desired = string.IsNullOrWhiteSpace(input.Slug) ? input.Title : input.Slug;
        var baseSlug = _slug.Generate(desired);
        if (isNew || string.IsNullOrEmpty(form.Slug))
        {
            form.Slug = await _slug.GenerateUniqueAsync(baseSlug,
                candidate => _db.FormDefinitions.AnyAsync(f => f.Slug == candidate && f.Id != form.Id, ct), ct);
        }
    }
}
