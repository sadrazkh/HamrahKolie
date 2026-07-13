using HamrahKolie.Application.Campaigns;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.Controllers;

public class CampaignsController : Controller
{
    private const int PageSize = 9;
    private readonly ICampaignService _campaigns;

    public CampaignsController(ICampaignService campaigns) => _campaigns = campaigns;

    [HttpGet("/campaigns")]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewData["Title"] = "کمپین‌ها";
        ViewData["Canonical"] = $"{Request.Scheme}://{Request.Host}/campaigns";
        var result = await _campaigns.GetPublishedListAsync(page, PageSize);
        return View(result);
    }

    [HttpGet("/campaigns/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var campaign = await _campaigns.GetPublishedBySlugAsync(slug);
        if (campaign is null) return NotFound();

        ViewData["Title"] = campaign.Seo.SeoTitle ?? campaign.Title;
        ViewData["MetaDescription"] = campaign.Seo.MetaDescription ?? campaign.ShortDescription;
        ViewData["Canonical"] = campaign.Seo.CanonicalUrl ?? $"{Request.Scheme}://{Request.Host}{Request.Path}";
        ViewData["OgImage"] = campaign.Seo.OgImageUrl ?? campaign.FeaturedImage?.Url;
        return View(campaign);
    }
}
