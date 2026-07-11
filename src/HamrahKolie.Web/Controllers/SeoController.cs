using System.Text;
using System.Xml;
using HamrahKolie.Application.Cms;
using HamrahKolie.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HamrahKolie.Web.Controllers;

/// <summary>خروجی‌های سئوی فنی: sitemap.xml و robots.txt</summary>
public class SeoController : Controller
{
    private readonly IContentService _content;

    public SeoController(IContentService content) => _content = content;

    [HttpGet("/sitemap.xml")]
    [OutputCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var items = await _content.GetAllPublishedForSitemapAsync();

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false), Async = false };
        await using (var sw = new StringWriter(sb))
        using (var xml = XmlWriter.Create(sw, settings))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // صفحات اصلی ثابت
            foreach (var path in new[] { "/", "/Home/About", "/Home/Services", "/news", "/articles" })
                WriteUrl(xml, baseUrl + path, null);

            // محتوای منتشرشده
            foreach (var c in items)
            {
                var path = c.Type switch
                {
                    ContentType.News => $"/news/{c.Slug}",
                    ContentType.Article => $"/articles/{c.Slug}",
                    ContentType.PatientStory => $"/stories/{c.Slug}",
                    ContentType.Page => $"/p/{c.Slug}",
                    _ => null
                };
                if (path is not null)
                    WriteUrl(xml, baseUrl + path, c.UpdatedAt ?? c.PublishedAt ?? c.CreatedAt);
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();
        }

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private static void WriteUrl(XmlWriter xml, string loc, DateTime? lastMod)
    {
        xml.WriteStartElement("url");
        xml.WriteElementString("loc", loc);
        if (lastMod is not null)
            xml.WriteElementString("lastmod", lastMod.Value.ToString("yyyy-MM-dd"));
        xml.WriteEndElement();
    }

    [HttpGet("/robots.txt")]
    [OutputCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /Admin");
        sb.AppendLine("Disallow: /Account");
        sb.AppendLine("Disallow: /jobs");
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");
        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
