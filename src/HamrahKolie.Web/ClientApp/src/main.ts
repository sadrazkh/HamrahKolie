import { createApp } from 'vue';
import StatsCounter from './islands/StatsCounter.vue';

// الگوی «جزیره‌های Vue»: هر عنصر با data-attribute مشخص به یک کامپوننت Vue تبدیل می‌شود.
// این‌گونه صفحات از سرور HTML کامل می‌گیرند (مناسب SEO) و فقط بخش‌های تعاملی با Vue فعال می‌شوند.

function mountStats() {
  const el = document.getElementById('island-stats');
  if (!el) return;
  let stats: { value: number; label: string }[] = [];
  try {
    stats = JSON.parse(el.dataset.stats ?? '[]');
  } catch {
    return;
  }
  el.innerHTML = '';
  createApp(StatsCounter, { stats }).mount(el);
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', mountStats);
} else {
  mountStats();
}
