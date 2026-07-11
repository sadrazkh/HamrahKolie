namespace HamrahKolie.Web.Areas.Admin.ViewModels;

public class DashboardViewModel
{
    public int UsersCount { get; set; }
    public int RolesCount { get; set; }
    public int PermissionsCount { get; set; }
    public int SettingsCount { get; set; }
    public int AuditLogsToday { get; set; }
    public IReadOnlyList<RecentAuditItem> RecentAudits { get; set; } = Array.Empty<RecentAuditItem>();
}

public record RecentAuditItem(DateTime OccurredAt, string Action, string? UserName, string? Description);
