<script setup lang="ts">
import { ref, watch } from 'vue';

interface FieldDef { name: string; label: string; }

const props = defineProps<{
  modelValue: string;
  itemsKey: string;      // "stats" | "cards"
  fields: FieldDef[];
}>();
const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

type Row = Record<string, string>;

function parse(): Row[] {
  try {
    const obj = JSON.parse(props.modelValue || '{}');
    const arr = obj[props.itemsKey];
    return Array.isArray(arr) ? arr.map((x) => ({ ...x })) : [];
  } catch {
    return [];
  }
}

const rows = ref<Row[]>(parse());

function emitChange() {
  emit('update:modelValue', JSON.stringify({ [props.itemsKey]: rows.value }));
}

watch(rows, emitChange, { deep: true });

function addRow() {
  const row: Row = {};
  props.fields.forEach((f) => (row[f.name] = ''));
  rows.value.push(row);
}
function removeRow(i: number) { rows.value.splice(i, 1); }
function move(i: number, dir: number) {
  const j = i + dir;
  if (j < 0 || j >= rows.value.length) return;
  const tmp = rows.value[i];
  rows.value[i] = rows.value[j];
  rows.value[j] = tmp;
}
</script>

<template>
  <div class="items-editor">
    <div v-for="(row, i) in rows" :key="i" class="items-editor__row">
      <div class="items-editor__fields">
        <label v-for="f in fields" :key="f.name" class="items-editor__field">
          <span>{{ f.label }}</span>
          <input v-model="row[f.name]" type="text" />
        </label>
      </div>
      <div class="items-editor__ops">
        <button type="button" @click="move(i, -1)" title="بالا">↑</button>
        <button type="button" @click="move(i, 1)" title="پایین">↓</button>
        <button type="button" class="danger" @click="removeRow(i)" title="حذف">✕</button>
      </div>
    </div>
    <button type="button" class="items-editor__add" @click="addRow">+ افزودن مورد</button>
    <p v-if="rows.length === 0" class="items-editor__empty">موردی وجود ندارد. یک مورد اضافه کنید.</p>
  </div>
</template>

<style>
.items-editor__row { display: flex; gap: 10px; align-items: flex-end; padding: 10px; border: 1px solid #e3e0d7; border-radius: 8px; margin-bottom: 8px; background: #fff; }
.items-editor__fields { display: flex; gap: 10px; flex: 1; flex-wrap: wrap; }
.items-editor__field { display: flex; flex-direction: column; gap: 4px; flex: 1; min-width: 140px; font-size: .85rem; font-weight: 600; }
.items-editor__field input { padding: 8px; border: 1.5px solid #e3e0d7; border-radius: 6px; font-family: inherit; font-size: .95rem; font-weight: 400; }
.items-editor__ops { display: flex; gap: 4px; }
.items-editor__ops button { border: 1px solid #e3e0d7; background: #f4f2ec; border-radius: 6px; padding: 6px 10px; cursor: pointer; }
.items-editor__ops button.danger { color: #c0392b; }
.items-editor__add { border: 1.5px dashed #86c99a; background: #eef7f0; color: #1f6f40; border-radius: 8px; padding: 8px 14px; cursor: pointer; font-family: inherit; font-weight: 700; }
.items-editor__empty { color: #5c6b63; font-size: .9rem; }
</style>
