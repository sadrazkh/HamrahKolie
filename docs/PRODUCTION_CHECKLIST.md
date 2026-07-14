# چک‌لیست استقرار Production — همراه کلیه

پیش از انتشار عمومی، موارد زیر را بررسی و تأیید کنید.

## 🔐 امنیت و اسرار
- [ ] `ASPNETCORE_ENVIRONMENT=Production` تنظیم شده باشد.
- [ ] رشته اتصال دیتابیس از **Environment Variable** یا Secret Store خوانده شود (نه داخل گیت).
- [ ] `SUPERADMIN_EMAIL` و `SUPERADMIN_PASSWORD` قوی تنظیم شده و پس از اولین ورود، رمز تغییر کند.
- [ ] `PresentationMode:Enabled=false` در Production (مسیر `/presentation/admin` فقط برای Development است).
- [ ] HTTPS اجباری فعال باشد (پشت Nginx با گواهی معتبر) و `UseHsts` فعال است.
- [ ] هدرهای امنیتی و CSP بررسی شوند (در `Program.cs`). در صورت افزودن اسکریپت/استایل خارجی، CSP به‌روزرسانی شود.
- [ ] Data Protection Keys پایدار شوند (Volume یا Redis) تا با ری‌استارت، کوکی/آنتی‌فورجری بی‌اعتبار نشود.
- [ ] Rate Limiting روی فرم‌های عمومی فعال است (`public-forms`).

## 🗄️ پایگاه داده
- [ ] Migrationها اعمال شده‌اند (`dotnet ef database update` یا اجرای خودکار در استارتاپ).
- [ ] پشتیبان‌گیری دوره‌ای دیتابیس تنظیم شده باشد.
- [ ] کاربر دیتابیس با حداقل دسترسی لازم (نه superuser) استفاده شود.

## 🚀 کارایی
- [ ] `npm run build` در ClientApp اجرا شده و `wwwroot/dist` موجود است (یا در Docker ساخته می‌شود).
- [ ] Output Cache و Response Compression فعال‌اند؛ باطل‌سازی کش با تگ «content» پس از انتشار محتوا کار می‌کند.
- [ ] فایل‌های استاتیک با Cache-Control مناسب سرو شوند (Nginx).
- [ ] برای بار بالا، Data Protection و (در صورت نیاز) Cache روی Redis منتقل شود (Interface آماده است).

## 🔔 سرویس‌های بیرونی (اختیاری تا زمان تنظیم)
- [ ] **درگاه پرداخت واقعی**: پیاده‌سازی `IPaymentGateway` و تنظیم `Payment:Provider`. تا آن زمان درگاه آزمایشی فعال است.
- [ ] **پیامک**: پیاده‌سازی `ISmsSender` + `IOtpSender` واقعی (برای OTP و اطلاع‌رسانی).
- [ ] **ایمیل**: پیاده‌سازی `IEmailSender` (SMTP).
- [ ] **ذخیره‌سازی**: در صورت نیاز، `IStorageService` به S3-Compatible منتقل شود.
- [ ] **نقشه**: `IMapLinkProvider` قابل تعویض است (پیش‌فرض OpenStreetMap).

## 📊 پایش
- [ ] `/health` توسط Load Balancer بررسی شود.
- [ ] لاگ‌های Serilog به مقصد پایدار (فایل/سرویس) هدایت شوند و اطلاعات حساس Mask شوند.
- [ ] داشبورد Hangfire (`/jobs`) فقط برای دسترسی «مدیریت فنی سیستم» باز است.

## 🧪 پیش از انتشار
- [ ] `dotnet test` سبز باشد.
- [ ] Sitemap (`/sitemap.xml`) و `robots.txt` بررسی شوند.
- [ ] صفحه ۴۰۴ و خطای عمومی درست نمایش داده شوند.
- [ ] Seed Data واقعی/محترمانه بررسی شود (بدون داده واقعی بیمار).

## ⚙️ موارد باقی‌مانده (غیرمسدودکننده)
- آپلود مستقیم مدرک درخواست حمایت و تصویر فیش پرداخت آفلاین.
- خروجی Excel گزارش‌ها و کمک‌ها.
- سخت‌ترکردن CSP با nonce به‌جای `unsafe-inline`.
