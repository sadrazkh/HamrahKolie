namespace HamrahKolie.Application.Common.Interfaces;

/// <summary>
/// تولید لینک نقشه/مسیریابی. Provider-Based تا بتوان سرویس نقشه را بدون تغییر کد عوض کرد
/// (پیش‌فرض: OpenStreetMap؛ بدون نیاز به کلید).
/// </summary>
public interface IMapLinkProvider
{
    string Name { get; }
    string? MapUrl(double? lat, double? lng, string? label = null);
    string? DirectionsUrl(double? lat, double? lng);
}
