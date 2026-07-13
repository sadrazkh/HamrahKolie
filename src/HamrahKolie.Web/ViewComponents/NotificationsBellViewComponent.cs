using HamrahKolie.Application.Common.Interfaces;
using HamrahKolie.Application.Notifications;
using HamrahKolie.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HamrahKolie.Web.ViewComponents;

public record BellModel(int UnreadCount, IReadOnlyList<Notification> Recent);

/// <summary>زنگوله اعلان‌های پنل: تعداد خوانده‌نشده + چند مورد اخیر.</summary>
public class NotificationsBellViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUser _currentUser;

    public NotificationsBellViewComponent(INotificationService notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return View(new BellModel(0, Array.Empty<Notification>()));

        try
        {
            var count = await _notifications.GetUnreadCountAsync(userId);
            var recent = await _notifications.GetRecentAsync(userId, 6);
            return View(new BellModel(count, recent));
        }
        catch
        {
            return View(new BellModel(0, Array.Empty<Notification>()));
        }
    }
}
