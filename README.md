# همراه کلیه — سامانه حمایت از بیماران دیالیزی

سامانه‌ی وب رسمی مؤسسه خیریه حمایت از بیماران کلیوی و دیالیزی، با تمرکز بر مناطق محروم و روستایی.
یک **Modular Monolith** با ASP.NET Core MVC + Razor و جزیره‌های **Vue 3** (نه SPA جدا)، فارسی و RTL-First.

> شعار: «هیچ مسیر سختی، با همراهی شما دور نخواهد بود.» — پیام اصلی: «از روستا تا زندگی».

---

## معماری

```
HamrahKolie.sln
├── src/
│   ├── HamrahKolie.Domain          موجودیت‌ها، Enumها، اینترفیس‌های پایه (بدون وابستگی)
│   ├── HamrahKolie.Application      سرویس‌ها، اینترفیس‌ها، کاتالوگ نقش/دسترسی، Validation
│   ├── HamrahKolie.Infrastructure   EF Core، Identity، تنظیمات، Audit، Seed، Providerها
│   └── HamrahKolie.Web              MVC + Razor + پنل مدیریت + ClientApp (Vite/Vue)
└── tests/
    └── HamrahKolie.Tests            آزمون‌ها
```

- **پایگاه داده:** PostgreSQL (پیش‌فرض) یا SQL Server — از `Database:Provider` قابل تغییر.
- **احراز هویت:** ASP.NET Core Identity + دسترسی مبتنی بر Permission (RBAC).
- **فرانت‌اند:** Vite + Vue 3 + TypeScript به‌صورت «جزیره‌های Vue»؛ خروجی در `wwwroot/dist`.
- **لاگ:** Serilog (کنسول + فایل در `logs/`). **کار پس‌زمینه:** Hangfire.

---

## پیش‌نیازها

- .NET SDK **10**
- Node.js **22** (برای Build فرانت‌اند)
- PostgreSQL **16+** (یا SQL Server)

---

## راه‌اندازی در محیط توسعه

### ۱) پایگاه داده
یک پایگاه داده در دسترس داشته باشید و رشته اتصال را در
`src/HamrahKolie.Web/appsettings.json` یا از طریق متغیر محیطی تنظیم کنید:

```
ConnectionStrings__Default=Host=localhost;Port=5432;Database=hamrahkolie;Username=postgres;Password=YOUR_PASSWORD
```

### ۲) حساب مدیر ارشد (بدون رمز ثابت در کد)
مقادیر زیر را به‌صورت متغیر محیطی تنظیم کنید تا در اولین اجرا ساخته شود:

```
SUPERADMIN_EMAIL=admin@example.com
SUPERADMIN_PASSWORD=YourStrongPassword#123
```

### ۳) Build فرانت‌اند (یک‌بار یا هنگام تغییر Vue)
```bash
cd src/HamrahKolie.Web/ClientApp
npm install
npm run build      # یا: npm run watch
```
> اگر این مرحله را اجرا نکنید، سایت همچنان بالا می‌آید و بخش‌های تعاملی نسخه‌ی ثابت (Fallback) را نشان می‌دهند.

### ۴) اجرای برنامه
```bash
dotnet run --project src/HamrahKolie.Web
```
- Migration و داده اولیه (نقش‌ها، دسترسی‌ها، تنظیمات) به‌صورت خودکار در راه‌اندازی اعمال می‌شوند.
- سایت عمومی: `https://localhost:5001` — پنل مدیریت: `/Admin` — ورود: `/Account/Login`.
- سلامت سیستم: `/health` — داشبورد Jobها: `/jobs` (نیازمند دسترسی فنی).

---

## اجرا با Docker

```bash
cp .env.example .env      # مقادیر را ویرایش کنید
docker compose up -d --build
```
سرویس‌ها: `web` (پورت ۸۰۸۰) و `db` (PostgreSQL). نمونه پیکربندی Nginx در `deploy/nginx.sample.conf`.

---

## دستورهای پرکاربرد

```bash
# Build کل Solution
dotnet build

# اجرای آزمون‌ها
dotnet test

# ساخت Migration جدید
dotnet ef migrations add <Name> \
  --project src/HamrahKolie.Infrastructure \
  --startup-project src/HamrahKolie.Web \
  --output-dir Persistence/Migrations
```

---

## قابلیت‌های پیاده‌سازی‌شده

**زیرساخت:** معماری Modular Monolith، EF Core + Identity + انتخاب Provider (PostgreSQL/SQL Server)، **RBAC مبتنی بر Permission** (۱۵ نقش، ~۴۰ دسترسی)، Audit Log، Serilog، Health Checks، Output Cache (با باطل‌سازی تگ‌محور)، Response Compression، Rate Limiting، Hangfire، هدرهای امنیتی + CSP، حذف نرم سراسری، Design System اختصاصی RTL، جزیره‌های Vue (Vite/TS).

**CMS (مرحله ۲):** محتوای یکپارچه (صفحه/خبر/مقاله/داستان بیمار)، دسته/برچسب، **کتابخانه رسانه**، منوها، ادیتور غنی **TipTap**، SEO کامل (Meta/OG/Canonical، JSON-LD، `/sitemap.xml`، `/robots.txt`).

**صفحه‌ساز (مرحله ۲+):** Page Section Builder سکشن‌محور با Drag & Drop، پیش‌نمایش و پیش‌نویس/انتشار.

**کمپین و کمک مالی (مرحله ۳):** کمپین‌ها، **درگاه پرداخت قابل‌تعویض** (`IPaymentGateway`) + درگاه آزمایشی، جریان کامل آنلاین با **Idempotency**، پرداخت آفلاین + صف بررسی، پیگیری و رسید، بازپرداخت.

**درخواست حمایت (مرحله ۴):** ثبت بدون حساب، **پیگیری با OTP**، گردش‌کار ۱۲ مرحله‌ای با تاریخچه، ارجاع/یادداشت/پیام، **حریم خصوصی سطح‌فیلد** (Masking).

**داوطلبان/مراکز/گزارش (مرحله ۵A):** ثبت‌نام داوطلب، دایرکتوری مراکز دیالیز با نقشه Provider-based، گزارش مدیریتی + صفحه عمومی **شفافیت مالی**.

**فرم‌ساز و اطلاع‌رسانی (مرحله ۵B):** فرم‌ساز داینامیک (۱۰ نوع فیلد)، سیستم اطلاع‌رسانی (in-app + ایمیل/پیامک با Template و Provider قابل‌تعویض).

**استقرار (مرحله ۶):** تنظیمات پنل، Docker/Compose/Nginx، CI، چک‌لیست Production ([docs/PRODUCTION_CHECKLIST.md](docs/PRODUCTION_CHECKLIST.md)).

**نیازمند سرویس/کلید بیرونی (Adapter آماده، بدون Credential فعال نمی‌شود):** درگاه پرداخت واقعی، پیامک/ایمیل واقعی، سرویس نقشه، Analytics خارجی.

---

## اصول مهم

- اطلاعات حساس بیماران جدا از داده عمومی نگه‌داری می‌شود و با دسترسی سطح‌فیلد کنترل خواهد شد.
- هیچ Secret واقعی در گیت قرار نمی‌گیرد؛ از Environment Variable یا `.env` استفاده کنید.
- محتوای آموزشی پزشکی جایگزین پزشک نیست و Disclaimer نمایش داده می‌شود.
