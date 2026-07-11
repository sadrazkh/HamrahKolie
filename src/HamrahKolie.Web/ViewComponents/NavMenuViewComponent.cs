using HamrahKolie.Domain.Enums;
using HamrahKolie.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HamrahKolie.Web.ViewComponents;

public record NavMenuItem(string Title, string Url, bool OpenInNewTab);

/// <summary>
/// منوی سرصفحه/پاورقی را از پایگاه داده می‌خواند (با Cache کوتاه).
/// اگر دیتابیس در دسترس نباشد، به فهرست ثابت پیش‌فرض برمی‌گردد تا سایت همیشه سالم باشد.
/// </summary>
public class NavMenuViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public NavMenuViewComponent(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync(string location = "Header")
    {
        var loc = string.Equals(location, "Footer", StringComparison.OrdinalIgnoreCase)
            ? MenuLocation.Footer : MenuLocation.Header;

        var items = await GetItemsAsync(loc);
        ViewData["Location"] = loc.ToString();
        return View(items);
    }

    private async Task<IReadOnlyList<NavMenuItem>> GetItemsAsync(MenuLocation loc)
    {
        try
        {
            return (await _cache.GetOrCreateAsync($"nav:{loc}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var items = await _db.MenuItems
                    .AsNoTracking()
                    .Where(mi => mi.Menu.Location == loc && mi.ParentId == null)
                    .OrderBy(mi => mi.SortOrder)
                    .Select(mi => new NavMenuItem(mi.Title, mi.Url, mi.OpenInNewTab))
                    .ToListAsync();
                return items.Count > 0 ? items : Fallback(loc);
            }))!;
        }
        catch
        {
            return Fallback(loc);
        }
    }

    private static IReadOnlyList<NavMenuItem> Fallback(MenuLocation loc) => loc == MenuLocation.Footer
        ? new List<NavMenuItem>
        {
            new("درباره مؤسسه", "/Home/About", false),
            new("اخبار", "/news", false),
            new("مقالات آموزشی", "/articles", false),
            new("حمایت مالی", "/Home/Donate", false),
        }
        : new List<NavMenuItem>
        {
            new("صفحه اصلی", "/", false),
            new("درباره ما", "/Home/About", false),
            new("خدمات حمایتی", "/Home/Services", false),
            new("اخبار", "/news", false),
            new("مقالات", "/articles", false),
            new("تماس با ما", "/Home/Contact", false),
        };
}
