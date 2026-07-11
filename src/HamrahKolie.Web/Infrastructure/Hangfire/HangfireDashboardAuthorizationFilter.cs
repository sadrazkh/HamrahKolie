using HamrahKolie.Application.Authorization;
using HamrahKolie.Infrastructure.Identity;
using Hangfire.Dashboard;

namespace HamrahKolie.Web.Infrastructure.Hangfire;

/// <summary>دسترسی به داشبورد Hangfire فقط برای کاربران دارای دسترسی «مدیریت فنی سیستم».</summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true) return false;

        return user.HasClaim(AppClaimTypes.Permission, "*")
            || user.HasClaim(AppClaimTypes.Permission, Permissions.SystemManage);
    }
}
