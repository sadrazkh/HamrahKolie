import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

// خروجی Build مستقیم داخل wwwroot/dist قرار می‌گیرد تا ASP.NET بتواند
// فایل‌های نهایی جزیره‌های Vue را از طریق manifest.json صدا بزند.
export default defineConfig({
  plugins: [vue()],
  base: '/dist/',
  build: {
    manifest: true,
    outDir: resolve(__dirname, '../wwwroot/dist'),
    emptyOutDir: true,
    rollupOptions: {
      input: resolve(__dirname, 'src/main.ts'),
    },
  },
});
