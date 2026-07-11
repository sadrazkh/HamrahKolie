<script setup lang="ts">
import { ref, onMounted } from 'vue';

interface Stat { value: number; label: string; }
const props = defineProps<{ stats: Stat[] }>();

const displayed = ref<number[]>(props.stats.map(() => 0));

// نمایش اعداد به‌صورت فارسی با جداکننده هزارگان.
function toFa(n: number): string {
  return Math.round(n).toLocaleString('fa-IR');
}

// انیمیشن شمارش تا مقدار نهایی.
function animate() {
  const duration = 1400;
  const start = performance.now();
  function frame(now: number) {
    const t = Math.min(1, (now - start) / duration);
    const eased = 1 - Math.pow(1 - t, 3);
    displayed.value = props.stats.map(s => s.value * eased);
    if (t < 1) requestAnimationFrame(frame);
  }
  requestAnimationFrame(frame);
}

onMounted(() => {
  const io = new IntersectionObserver((entries) => {
    if (entries.some(e => e.isIntersecting)) { animate(); io.disconnect(); }
  }, { threshold: 0.3 });
  const el = document.getElementById('island-stats');
  if (el) io.observe(el); else animate();
});
</script>

<template>
  <div class="grid grid-4" style="margin-top:2rem">
    <div class="stat" v-for="(s, i) in stats" :key="i">
      <div class="stat-value">{{ toFa(displayed[i]) }}</div>
      <div class="stat-label">{{ s.label }}</div>
    </div>
  </div>
</template>
