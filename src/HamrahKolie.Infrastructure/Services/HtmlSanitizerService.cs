using HamrahKolie.Application.Common.Interfaces;
using Ganss.Xss;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>پاک‌سازی HTML با کتابخانه HtmlSanitizer (جلوگیری از XSS و تگ‌های خطرناک).</summary>
public sealed class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();

        // تگ‌ها و ویژگی‌های مجاز برای محتوای غنی سایت.
        _sanitizer.AllowedTags.UnionWith(new[]
        {
            "h1", "h2", "h3", "h4", "p", "br", "hr", "blockquote",
            "ul", "ol", "li", "strong", "em", "u", "s", "a", "img",
            "table", "thead", "tbody", "tr", "th", "td", "figure", "figcaption",
            "span", "div", "code", "pre"
        });
        _sanitizer.AllowedAttributes.UnionWith(new[] { "href", "src", "alt", "title", "class", "target", "rel", "dir", "colspan", "rowspan" });
        _sanitizer.AllowedSchemes.UnionWith(new[] { "http", "https", "mailto", "tel" });

        // لینک‌های خارجی امن باز شوند.
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Html.Dom.IHtmlAnchorElement a &&
                a.GetAttribute("target") == "_blank")
            {
                a.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? string.Empty : _sanitizer.Sanitize(html);
}
