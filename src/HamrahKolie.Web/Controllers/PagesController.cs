using HamrahKolie.Application.Cms;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Controllers;

/// <summary>نمایش صفحات ثابت (Page) بر اساس نامک: /p/{slug}</summary>
public class PagesController : Controller
{
    private readonly IContentService _content;
    private readonly IPageBuilderService _pageBuilder;

    public PagesController(IContentService content, IPageBuilderService pageBuilder)
    {
        _content = content;
        _pageBuilder = pageBuilder;
    }

    [HttpGet("/p/{slug}")]
    public async Task<IActionResult> View(string slug)
    {
        var page = await _content.GetPublishedBySlugAsync(ContentType.Page, slug);
        if (page is null)
        {
            return NotFound();
        }

        ViewData["Title"] = page.Seo.SeoTitle ?? page.Title;
        ViewData["MetaDescription"] = page.Seo.MetaDescription ?? page.Summary;
        ViewData["Canonical"] = page.Seo.CanonicalUrl ?? $"{Request.Scheme}://{Request.Host}{Request.Path}";
        if (page.Seo.NoIndex)
        {
            ViewData["Robots"] = "noindex,follow";
        }

        try
        {
            var sections = await _pageBuilder.GetVisibleAsync($"page:{slug.ToLowerInvariant()}");
            if (sections.Count > 0)
            {
                return View("~/Views/Shared/BuilderPage.cshtml", new HamrahKolie.Web.Areas.Admin.ViewModels.PageBuilderCanvasViewModel
                {
                    PageKey = $"page:{slug.ToLowerInvariant()}",
                    PageTitle = page.Title,
                    Sections = sections,
                });
            }
        }
        catch { }

        return View("~/Views/Blog/Detail.cshtml", page);
    }
}
