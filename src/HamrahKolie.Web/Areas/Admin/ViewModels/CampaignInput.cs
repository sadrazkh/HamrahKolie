using System.ComponentModel.DataAnnotations;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Web.Areas.Admin.ViewModels;

public class CampaignInput
{
    public long Id { get; set; }

    [Required(ErrorMessage = "عنوان را وارد کنید.")]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }

    [Range(0, 1_000_000_000_000, ErrorMessage = "مبلغ هدف نامعتبر است.")]
    public decimal GoalAmount { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public bool IsUrgent { get; set; }
    public bool ShowExactAmount { get; set; } = true;

    public string? Province { get; set; }
    public string? City { get; set; }
    public string? NeedType { get; set; }

    public long? FeaturedImageId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal? MinDonation { get; set; }
    public decimal? MaxDonation { get; set; }

    // فقط برای نمایش
    public decimal CollectedAmount { get; set; }
    public int SupporterCount { get; set; }
}
