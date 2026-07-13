using System.Globalization;
using HamrahKolie.Application.Common.Interfaces;

namespace HamrahKolie.Infrastructure.Services;

/// <summary>تولید لینک نقشه با OpenStreetMap (بدون نیاز به کلید). قابل تعویض با سرویس دیگر.</summary>
public sealed class OpenStreetMapLinkProvider : IMapLinkProvider
{
    public string Name => "OpenStreetMap";

    public string? MapUrl(double? lat, double? lng, string? label = null)
    {
        if (lat is null || lng is null) return null;
        var la = lat.Value.ToString(CultureInfo.InvariantCulture);
        var lo = lng.Value.ToString(CultureInfo.InvariantCulture);
        return $"https://www.openstreetmap.org/?mlat={la}&mlon={lo}#map=16/{la}/{lo}";
    }

    public string? DirectionsUrl(double? lat, double? lng)
    {
        if (lat is null || lng is null) return null;
        var la = lat.Value.ToString(CultureInfo.InvariantCulture);
        var lo = lng.Value.ToString(CultureInfo.InvariantCulture);
        return $"https://www.openstreetmap.org/directions?to={la},{lo}";
    }
}
