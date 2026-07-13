namespace HamrahKolie.Application.Reports;

public record MoneyStat(decimal Amount, int Count);

public record CampaignProgress(string Title, string Slug, decimal Goal, decimal Collected, int Supporters, int Percent);

public record LabeledCount(string Label, int Count);

/// <summary>گزارش مدیریتی جامع.</summary>
public class ManagementReport
{
    public MoneyStat Today { get; set; } = new(0, 0);
    public MoneyStat ThisWeek { get; set; } = new(0, 0);
    public MoneyStat ThisMonth { get; set; } = new(0, 0);
    public MoneyStat ThisYear { get; set; } = new(0, 0);
    public MoneyStat AllTime { get; set; } = new(0, 0);

    public MoneyStat Online { get; set; } = new(0, 0);
    public MoneyStat Offline { get; set; } = new(0, 0);

    public int NewDonors { get; set; }
    public int RepeatDonors { get; set; }
    public decimal AverageDonation { get; set; }

    public IReadOnlyList<CampaignProgress> Campaigns { get; set; } = Array.Empty<CampaignProgress>();
    public IReadOnlyList<LabeledCount> SupportRequestsByStatus { get; set; } = Array.Empty<LabeledCount>();
    public IReadOnlyList<LabeledCount> SupportRequestsByProvince { get; set; } = Array.Empty<LabeledCount>();
    public IReadOnlyList<LabeledCount> VolunteersByStatus { get; set; } = Array.Empty<LabeledCount>();
}

/// <summary>گزارش عمومی شفافیت (بدون اطلاعات محرمانه).</summary>
public class TransparencyReport
{
    public decimal TotalRaised { get; set; }
    public int TotalDonations { get; set; }
    public int ActiveCampaigns { get; set; }
    public int PatientsSupported { get; set; }
    public int RegisteredVolunteers { get; set; }
    public IReadOnlyList<CampaignProgress> Campaigns { get; set; } = Array.Empty<CampaignProgress>();
}

public interface IReportService
{
    Task<ManagementReport> GetManagementReportAsync(CancellationToken ct = default);
    Task<TransparencyReport> GetTransparencyReportAsync(CancellationToken ct = default);
}
