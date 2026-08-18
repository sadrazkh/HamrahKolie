using HamrahKolie.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace HamrahKolie.Tests.Configuration;

/// <summary>
/// از کجا رشته اتصال خوانده می‌شود.
///
/// اپ همیشه <c>ConnectionStrings:Default</c> را می‌خواند، ولی پلتفرم‌های میزبانی این نام را
/// نمی‌دانند: هاربورا هنگام اتصال دیتابیس، <c>ConnectionStrings__DefaultConnection</c> و
/// <c>DATABASE_DSN</c> را می‌نویسد. نتیجه‌اش این بود که اپ روی سرور بالا می‌آمد، مقدار
/// appsettings.json را برمی‌داشت — که داخل کانتینر یعنی localhost — و health check شکست می‌خورد
/// و کل دیپلوی Failed علامت می‌خورد، بدون اینکه هیچ لاگی اسم دیتابیس را ببرد.
/// </summary>
public class ConnectionStringSourcesTests
{
    private static IConfiguration Config(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.ToDictionary(p => p.Key, p => (string?)p.Value))
            .Build();

    [Fact]
    public void مقدار_خود_اپ_بالاترین_اولویت_را_دارد()
    {
        var config = Config(
            ("ConnectionStrings:Default", "Host=chosen"),
            ("ConnectionStrings:DefaultConnection", "Host=platform"),
            ("DATABASE_DSN", "Host=platform-dsn"));

        Assert.Equal("Host=chosen", ConnectionStringSources.Resolve(config));
    }

    [Fact]
    public void نام_پیشفرض_دات_نت_وقتی_کلید_خود_اپ_خالی_است_خوانده_میشود()
    {
        // چیزی که هاربورا هنگام attach می‌نویسد. بدون این، اپ باید یک متغیر دستی می‌گرفت.
        var config = Config(
            ("ConnectionStrings:Default", ""),
            ("ConnectionStrings:DefaultConnection", "Host=db;Port=5432;Database=k;Username=u;Password=p"));

        Assert.Equal("Host=db;Port=5432;Database=k;Username=u;Password=p",
            ConnectionStringSources.Resolve(config));
    }

    [Fact]
    public void DATABASE_DSN_آخرین_جایی_است_که_نگاه_میشود()
    {
        var config = Config(("DATABASE_DSN", "Host=db;Port=5432;Database=k;Username=u;Password=p"));

        Assert.Equal("Host=db;Port=5432;Database=k;Username=u;Password=p",
            ConnectionStringSources.Resolve(config));
    }

    [Fact]
    public void مقدار_فقط_فضای_خالی_مثل_نبودن_رفتار_میکند()
    {
        // appsettings.json با یک رشته خالی commit شده است تا متغیر محیطی جایش را بگیرد؛
        // اگر خالی «یک مقدار» حساب می‌شد، آن فایل برای همیشه جلوی پلتفرم را می‌گرفت.
        var config = Config(
            ("ConnectionStrings:Default", "   "),
            ("ConnectionStrings:DefaultConnection", "Host=db"));

        Assert.Equal("Host=db", ConnectionStringSources.Resolve(config));
    }

    [Fact]
    public void وقتی_هیچ_کدام_نیست_نتیجه_تهی_است()
    {
        // اپ عمداً با دیتابیس در دسترس‌نبودن هم بالا می‌آید، پس اینجا استثنا انداختن غلط است.
        Assert.Null(ConnectionStringSources.Resolve(Config()));
    }

    [Fact]
    public void یک_URI_به_جای_رشته_اتصال_پذیرفته_نمیشود()
    {
        // DATABASE_URL شکل postgresql://… دارد و Npgsql فقط keyword=value را می‌فهمد. قبول کردنش
        // یعنی اپ بالا می‌آید و سر اولین کوئری می‌افتد — دقیقاً همان شکست بی‌نام قبلی.
        var config = Config(("DATABASE_DSN", "postgresql://u:p@db:5432/k"));

        Assert.Null(ConnectionStringSources.Resolve(config));
    }
}
