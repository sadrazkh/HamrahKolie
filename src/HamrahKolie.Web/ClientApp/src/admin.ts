import { createApp, h, ref, watch } from 'vue';
import RichEditor from './islands/RichEditor.vue';
import ItemsEditor from './islands/ItemsEditor.vue';

// ادیتور غنی محتوا: هر عنصر با data-editor-target به یک ادیتور TipTap تبدیل می‌شود
// و مقدار HTML را با یک <textarea> مخفی (که در فرم submit می‌شود) همگام نگه می‌دارد.
function mountEditors() {
  document.querySelectorAll<HTMLElement>('[data-editor-target]').forEach((el) => {
    const targetId = el.dataset.editorTarget!;
    const textarea = document.getElementById(targetId) as HTMLTextAreaElement | null;
    if (!textarea) return;

    const model = ref(textarea.value);
    watch(model, (val) => { textarea.value = val; });

    createApp({
      render: () => h(RichEditor, {
        modelValue: model.value,
        'onUpdate:modelValue': (v: string) => { model.value = v; },
      }),
    }).mount(el);
  });
}

// ادیتور آیتم‌ها (برای Stats/FeatureCards/Steps در صفحه‌ساز)
function mountItemsEditors() {
  document.querySelectorAll<HTMLElement>('[data-items-editor]').forEach((el) => {
    const targetId = el.dataset.itemsEditor!;
    const itemsKey = el.dataset.itemsKey || 'items';
    const textarea = document.getElementById(targetId) as HTMLTextAreaElement | null;
    if (!textarea) return;

    let fields: { name: string; label: string }[] = [];
    try { fields = JSON.parse(el.dataset.itemsFields || '[]'); } catch { fields = []; }

    const model = ref(textarea.value || '{}');
    watch(model, (val) => { textarea.value = val; });

    createApp({
      render: () => h(ItemsEditor, {
        modelValue: model.value,
        itemsKey,
        fields,
        'onUpdate:modelValue': (v: string) => { model.value = v; },
      }),
    }).mount(el);
  });
}

function mountAll() {
  mountEditors();
  mountItemsEditors();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', mountAll);
} else {
  mountAll();
}
