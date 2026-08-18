using Microsoft.Extensions.Configuration;

namespace HamrahKolie.Infrastructure.Configuration;

/// <summary>
/// رشته اتصال از کجا می‌آید، وقتی میزبان نام دیگری برایش انتخاب کرده است.
///
/// اپ همه‌جا <c>ConnectionStrings:Default</c> را می‌خواند. پلتفرم‌های میزبانی این نام را نمی‌دانند:
/// هاربورا هنگام اتصال یک دیتابیس <c>ConnectionStrings__DefaultConnection</c> (نام قراردادی .NET)
/// و <c>DATABASE_DSN</c> را می‌نویسد. تا پیش از این، اپ هیچ‌کدام را نمی‌دید، مقدار appsettings.json
/// را برمی‌داشت — که داخل کانتینر localhost است — و health check شکست می‌خورد؛ دیپلوی Failed
/// علامت می‌خورد بدون آنکه هیچ لاگی اسم دیتابیس یا اتصال را ببرد.
///
/// ترتیب عمدی است: مقدار صریح خود پروژه همیشه برنده است، وگرنه چیزی که پلتفرم نوشته خوانده می‌شود.
/// </summary>
public static class ConnectionStringSources
{
    /// <summary>کلیدی که تمام کد اپ از آن می‌خواند.</summary>
    public const string PrimaryKey = "ConnectionStrings:Default";

    /// <summary>
    /// اولین مقدار معتبر از میان کلید خود اپ، نام قراردادی .NET، و متغیر خام پلتفرم.
    /// اگر هیچ‌کدام نبود <c>null</c> — چون اپ عمداً با دیتابیس در دسترس‌نبودن هم بالا می‌آید.
    /// </summary>
    public static string? Resolve(IConfiguration config) => FirstUsable(
        config.GetConnectionString("Default"),
        config.GetConnectionString("DefaultConnection"),
        config["DATABASE_DSN"]);

    private static string? FirstUsable(params string?[] candidates) =>
        candidates.FirstOrDefault(IsConnectionString);

    /// <summary>
    /// یک URI رشته اتصال نیست. <c>DATABASE_URL</c> به شکل <c>postgresql://…</c> است و هیچ درایور
    /// ADO.NET آن را نمی‌فهمد؛ پذیرفتنش یعنی اپ بالا بیاید و سر اولین کوئری بیفتد — همان شکست
    /// بی‌نامی که این کلاس برای بستنش نوشته شده. رد کردنش اینجا یعنی مسیر «دیتابیس تنظیم نشده»
    /// که پیام روشن دارد.
    /// </summary>
    private static bool IsConnectionString(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Contains("://", StringComparison.Ordinal);
}
