namespace HamrahKolie.Web.Areas.Admin.ViewModels;

/// <summary>مدل ویرایش تنظیمات اصلی مؤسسه (نگاشت به کلیدهای Setting).</summary>
public class SettingsViewModel
{
    public string? OrganizationName { get; set; }
    public string? Slogan { get; set; }
    public string? Message { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }

    public string? OfflineAccount { get; set; }

    public bool MaintenanceMode { get; set; }
}
