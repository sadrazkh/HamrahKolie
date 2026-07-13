using HamrahKolie.Domain.Common;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Domain.Entities;

/// <summary>یک کمپین جمع‌آوری کمک.</summary>
public class Campaign : BaseEntity
{
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }   // HTML

    public long? FeaturedImageId { get; set; }
    public MediaFile? FeaturedImage { get; set; }

    /// <summary>مبلغ هدف (تومان).</summary>
    public decimal GoalAmount { get; set; }

    /// <summary>مبلغ جمع‌آوری‌شده (کش‌شده؛ با هر کمک موفق به‌روزرسانی می‌شود).</summary>
    public decimal CollectedAmount { get; set; }

    /// <summary>تعداد حامیان (کش‌شده).</summary>
    public int SupporterCount { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public bool IsUrgent { get; set; }

    public string? Province { get; set; }
    public string? City { get; set; }
    public string? NeedType { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    /// <summary>آیا مبلغ دقیق جمع‌آوری‌شده به‌صورت عمومی نمایش داده شود؟</summary>
    public bool ShowExactAmount { get; set; } = true;

    public decimal? MinDonation { get; set; }
    public decimal? MaxDonation { get; set; }

    public SeoMetadata Seo { get; set; } = new();

    public ICollection<CampaignUpdate> Updates { get; set; } = new List<CampaignUpdate>();

    /// <summary>درصد پیشرفت (۰ تا ۱۰۰).</summary>
    public int ProgressPercent => GoalAmount <= 0 ? 0
        : (int)Math.Min(100, Math.Round(CollectedAmount / GoalAmount * 100));

    public bool IsPubliclyVisible => Status is CampaignStatus.Active or CampaignStatus.Successful or CampaignStatus.Completed;
}

/// <summary>به‌روزرسانی/خبر یک کمپین.</summary>
public class CampaignUpdate : BaseEntity
{
    public long CampaignId { get; set; }
    public Campaign Campaign { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Body { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}
