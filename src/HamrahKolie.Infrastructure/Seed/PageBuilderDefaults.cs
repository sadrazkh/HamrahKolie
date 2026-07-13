using System.Text.Json;
using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Entities;
using HamrahKolie.Domain.Enums;

namespace HamrahKolie.Infrastructure.Seed;

/// <summary>قالب‌های اولیه برای شروع ویرایش بصری هر صفحه.</summary>
public static class PageBuilderDefaults
{
    public static List<PageSection> Build(string pageKey)
    {
        if (pageKey == "home") return DbSeeder.BuildDefaultHomeSections();

        var title = pageKey switch
        {
            "about" => "درباره همراه کلیه",
            "services" => "خدمات حمایتی",
            "campaigns" => "کمپین‌های همراهی",
            "donate" => "حمایت مالی",
            "contact" => "تماس با ما",
            _ when pageKey.StartsWith("page:") => pageKey[5..].Replace('-', ' '),
            _ => "صفحه جدید",
        };
        var subtitle = pageKey switch
        {
            "about" => "داستان شکل‌گیری، مأموریت و ارزش‌های مؤسسه همراه کلیه",
            "services" => "از رفت‌وآمد و درمان تا حمایت خانواده؛ در کنار بیمار می‌مانیم.",
            "campaigns" => "با مشارکت در کمپین‌ها، مستقیماً در تأمین نیازهای بیماران سهیم شوید.",
            "donate" => "هر همراهی، یک قدم به بازگشت بیمار به زندگی نزدیک‌تر است.",
            "contact" => "برای پرسش، پیشنهاد یا آغاز همکاری با ما در ارتباط باشید.",
            _ => "محتوای این صفحه را از ویرایشگر بصری تکمیل کنید.",
        };

        return new List<PageSection>
        {
            Section(pageKey, SectionType.Hero, 1, title, subtitle, style: new SectionStyle(PaddingTop: 88, PaddingBottom: 88, TextAlign: "center", BackgroundColor: "#eef7f0")),
            Section(pageKey, SectionType.RichText, 2, "محتوای صفحه", body: "<p>برای ویرایش این متن، سکشن را در بوم انتخاب کنید و محتوای دلخواه را بنویسید.</p>"),
            Section(pageKey, SectionType.CallToAction, 3, "همراه ما باشید", "با همراهی شما مسیر درمان کوتاه‌تر می‌شود.", "حمایت مالی", "/donate", style: new SectionStyle(BackgroundColor: "#176b9b", TextColor: "#ffffff", TextAlign: "center", BorderRadius: 20)),
        };
    }

    private static PageSection Section(
        string pageKey,
        SectionType type,
        int order,
        string? title = null,
        string? subtitle = null,
        string? buttonText = null,
        string? buttonUrl = null,
        string? body = null,
        SectionStyle? style = null)
        => new()
        {
            PageKey = pageKey,
            Type = type,
            SortOrder = order,
            IsEnabled = true,
            IsPublished = true,
            ShowOnDesktop = true,
            ShowOnMobile = true,
            Title = title,
            Subtitle = subtitle,
            ButtonText = buttonText,
            ButtonUrl = buttonUrl,
            Body = body,
            SettingsJson = JsonSerializer.Serialize(new { style = style ?? new SectionStyle() }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        };
}
