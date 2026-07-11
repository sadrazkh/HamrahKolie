using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Web.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Content> LatestNews { get; set; } = Array.Empty<Content>();
    public IReadOnlyList<Content> LatestArticles { get; set; } = Array.Empty<Content>();
}
