using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Tests.Donations;

public class CampaignTests
{
    [Theory]
    [InlineData(0, 100, 0)]       // هدف صفر → درصد صفر (بدون تقسیم بر صفر)
    [InlineData(1000, 0, 0)]
    [InlineData(1000, 500, 50)]
    [InlineData(1000, 1000, 100)]
    [InlineData(1000, 2000, 100)] // سقف ۱۰۰
    public void ProgressPercent_is_calculated_and_capped(decimal goal, decimal collected, int expected)
    {
        var c = new Campaign { GoalAmount = goal, CollectedAmount = collected };
        Assert.Equal(expected, c.ProgressPercent);
    }

    [Theory]
    [InlineData(CampaignStatus.Active, true)]
    [InlineData(CampaignStatus.Successful, true)]
    [InlineData(CampaignStatus.Completed, true)]
    [InlineData(CampaignStatus.Draft, false)]
    [InlineData(CampaignStatus.Paused, false)]
    [InlineData(CampaignStatus.Closed, false)]
    public void IsPubliclyVisible_matches_status(CampaignStatus status, bool expected)
    {
        var c = new Campaign { Status = status };
        Assert.Equal(expected, c.IsPubliclyVisible);
    }
}
