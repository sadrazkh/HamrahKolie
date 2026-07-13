using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Web.Helpers;

public static class Phase5Display
{
    public static string Collaboration(CollaborationType t) => t switch
    {
        CollaborationType.Medical => "پزشکی",
        CollaborationType.Nursing => "پرستاری",
        CollaborationType.SocialWork => "مددکاری",
        CollaborationType.Psychology => "روان‌شناسی",
        CollaborationType.Transport => "حمل‌ونقل",
        CollaborationType.ContentCreation => "تولید محتوا",
        CollaborationType.Design => "طراحی",
        CollaborationType.Technology => "فناوری",
        CollaborationType.Photography => "عکاسی",
        CollaborationType.EventOrganizing => "برگزاری رویداد",
        CollaborationType.Fundraising => "جمع‌آوری کمک",
        CollaborationType.Organizational => "همکاری سازمانی",
        _ => "سایر"
    };

    public static string VolunteerStatus(VolunteerStatus s) => s switch
    {
        Domain.Enums.VolunteerStatus.Pending => "در انتظار بررسی",
        Domain.Enums.VolunteerStatus.Approved => "تأییدشده",
        Domain.Enums.VolunteerStatus.Active => "فعال",
        Domain.Enums.VolunteerStatus.Inactive => "غیرفعال",
        Domain.Enums.VolunteerStatus.Blacklisted => "لیست سیاه",
        _ => s.ToString()
    };

    public static string CenterType(CenterType t) => t switch
    {
        Domain.Enums.CenterType.Governmental => "دولتی",
        Domain.Enums.CenterType.Private => "خصوصی",
        Domain.Enums.CenterType.Charity => "خیریه",
        Domain.Enums.CenterType.University => "دانشگاهی",
        _ => "سایر"
    };
}
