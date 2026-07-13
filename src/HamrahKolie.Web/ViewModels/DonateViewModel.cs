using HamrahKolie.Application.Donations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HamrahKolie.Web.ViewModels;

public class DonateViewModel
{
    public DonationInput Input { get; set; } = new();
    public IReadOnlyList<SelectListItem> Campaigns { get; set; } = new List<SelectListItem>();
    public string? SelectedCampaignTitle { get; set; }

    /// <summary>مبلغ‌های پیشنهادی (تومان).</summary>
    public static readonly long[] SuggestedAmounts = { 100_000, 200_000, 500_000, 1_000_000 };
}

public class OfflineDonateViewModel
{
    public OfflineDonationInput Input { get; set; } = new();
    public IReadOnlyList<SelectListItem> Campaigns { get; set; } = new List<SelectListItem>();
    public string? BankAccountInfo { get; set; }
}

public class TrackDonationViewModel
{
    public string TrackingCode { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public HamrahKolie.Domain.Entities.Donation? Result { get; set; }
    public bool Searched { get; set; }
}
