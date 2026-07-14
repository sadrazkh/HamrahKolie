using HamrahKolie.Application.Cms;
using HamrahKolie.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Controllers;

/// <summary>فهرست و جزئیات محتوای عمومی (اخبار، مقالات، داستان‌ها). صفحات کاملاً سمت سرور رندر می‌شوند.</summary>
[OutputCache(PolicyName = "PublicContent")]
public class BlogController : Controller
{
    private const int PageSize = 9;
    private readonly IContentService _content;

    public BlogController(IContentService content) => _content = content;

    // ── اخبار ────────────────────────────────────────────────────
    [HttpGet("/news")]
    public Task<IActionResult> News(int page = 1, string? category = null, string? tag = null)
        => ListAsync(ContentType.News, "اخبار", "news", page, category, tag);

    [HttpGet("/news/{slug}")]
    public Task<IActionResult> NewsDetail(string slug) => DetailAsync(ContentType.News, slug);

    // ── مقالات ───────────────────────────────────────────────────
    [HttpGet("/articles")]
    public Task<IActionResult> Articles(int page = 1, string? category = null, string? tag = null)
        => ListAsync(ContentType.Article, "مقالات آموزشی", "articles", page, category, tag);

    [HttpGet("/articles/{slug}")]
    public Task<IActionResult> ArticleDetail(string slug) => DetailAsync(ContentType.Article, slug);

    // ── داستان‌های امید ──────────────────────────────────────────
    [HttpGet("/stories")]
    public Task<IActionResult> Stories(int page = 1)
        => ListAsync(ContentType.PatientStory, "داستان‌های امید", "stories", page, null, null);

    [HttpGet("/stories/{slug}")]
    public Task<IActionResult> StoryDetail(string slug) => DetailAsync(ContentType.PatientStory, slug);

    // ── مشترک ────────────────────────────────────────────────────
    private async Task<IActionResult> ListAsync(
        ContentType type, string title, string routeBase, int page, string? category, string? tag)
    {
        var result = await _content.GetPublishedListAsync(type, category, tag, page, PageSize);
        ViewData["Title"] = title;
        ViewData["RouteBase"] = routeBase;
        ViewData["ContentType"] = type;
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}/{routeBase}";
        return View("List", result);
    }

    private async Task<IActionResult> DetailAsync(ContentType type, string slug)
    {
        var item = await _content.GetPublishedBySlugAsync(type, slug);
        if (item is null) return NotFound();

        ViewData["Title"] = item.Seo.SeoTitle ?? item.Title;
        ViewData["MetaDescription"] = item.Seo.MetaDescription ?? item.Summary;
        ViewData["Canonical"] = item.Seo.CanonicalUrl ?? $"{Request.Scheme}://{Request.Host}{Request.Path}";
        ViewData["OgImage"] = item.Seo.OgImageUrl ?? item.FeaturedImage?.Url;
        ViewData["OgType"] = "article";
        if (item.Seo.NoIndex) ViewData["Robots"] = "noindex,follow";
        return View("Detail", item);
    }
}
