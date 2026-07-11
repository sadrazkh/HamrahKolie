using System.Globalization;
using System.Text;
using HamrahKolie.Application.Common.Interfaces;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>ساخت نامک با پشتیبانی از فارسی و نرمال‌سازی حروف عربی/فارسی.</summary>
public sealed class SlugService : ISlugService
{
    public string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = NormalizePersian(input).Trim().ToLowerInvariant();

        var sb = new StringBuilder(normalized.Length);
        var lastDash = false;
        foreach (var ch in normalized)
        {
            if (IsAllowed(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '/' or '\\')
            {
                if (!lastDash && sb.Length > 0) { sb.Append('-'); lastDash = true; }
            }
            // سایر نویسه‌ها (نقطه‌گذاری) حذف می‌شوند.
        }

        return sb.ToString().Trim('-');
    }

    public async Task<string> GenerateUniqueAsync(
        string input, Func<string, Task<bool>> existsAsync, CancellationToken ct = default)
    {
        var baseSlug = Generate(input);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "item";

        var candidate = baseSlug;
        var i = 2;
        while (await existsAsync(candidate))
        {
            candidate = $"{baseSlug}-{i}";
            i++;
        }
        return candidate;
    }

    /// <summary>یکسان‌سازی «ي/ك» عربی به «ی/ک» فارسی و حذف اعراب و کشیده.</summary>
    private static string NormalizePersian(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case 'ي': sb.Append('ی'); break;
                case 'ك': sb.Append('ک'); break;
                case 'ـ': break; // کشیده
                case 'ً': case 'ٌ': case 'ٍ':
                case 'َ': case 'ُ': case 'ِ':
                case 'ّ': case 'ْ': break; // اعراب
                case '‌': sb.Append(' '); break; // نیم‌فاصله → فاصله
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    private static bool IsAllowed(char ch)
    {
        if (ch is >= 'a' and <= 'z') return true;
        if (ch is >= '0' and <= '9') return true;
        // بازه حروف فارسی/عربی
        if (ch is >= 'ء' and <= 'ی') return true;
        var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
        return cat is UnicodeCategory.LowercaseLetter or UnicodeCategory.OtherLetter;
    }
}
