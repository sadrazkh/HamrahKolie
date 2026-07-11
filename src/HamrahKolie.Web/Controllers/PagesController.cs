using HamrahKolie.Application.Cms;
using HamrahKolie.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Controllers;

/// <summary>نمایش صفحات ثابت (Page) بر اساس نامک: /p/{slug}</summary>
public class PagesController : Controller
{
    private readonly IContentService _content;

    public PagesController(IContentService content) => _content = content;

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

        return View("~/Views/Blog/Detail.cshtml", page);
    }
}
