<script setup lang="ts">
import { watch, onBeforeUnmount } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import Link from '@tiptap/extension-link';
import Image from '@tiptap/extension-image';

const props = defineProps<{ modelValue: string }>();
const emit = defineEmits<{ (e: 'update:modelValue', value: string): void }>();

const editor = useEditor({
  content: props.modelValue || '',
  extensions: [
    StarterKit,
    Link.configure({ openOnClick: false, HTMLAttributes: { rel: 'noopener noreferrer' } }),
    Image.configure({ inline: false }),
  ],
  onUpdate: ({ editor }) => emit('update:modelValue', editor.getHTML()),
});

watch(() => props.modelValue, (val) => {
  if (editor.value && val !== editor.value.getHTML()) {
    editor.value.commands.setContent(val || '', false);
  }
});

onBeforeUnmount(() => editor.value?.destroy());

function addLink() {
  const url = window.prompt('نشانی لینک را وارد کنید:');
  if (url) editor.value?.chain().focus().setLink({ href: url }).run();
}
function addImage() {
  const url = window.prompt('نشانی تصویر را وارد کنید (از کتابخانه رسانه کپی کنید):');
  if (url) editor.value?.chain().focus().setImage({ src: url }).run();
}
</script>

<template>
  <div class="rich-editor" v-if="editor">
    <div class="rich-toolbar">
      <button type="button" @click="editor.chain().focus().toggleBold().run()" :class="{ active: editor.isActive('bold') }"><b>B</b></button>
      <button type="button" @click="editor.chain().focus().toggleItalic().run()" :class="{ active: editor.isActive('italic') }"><i>I</i></button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()" :class="{ active: editor.isActive('heading', { level: 2 }) }">H2</button>
      <button type="button" @click="editor.chain().focus().toggleHeading({ level: 3 }).run()" :class="{ active: editor.isActive('heading', { level: 3 }) }">H3</button>
      <button type="button" @click="editor.chain().focus().toggleBulletList().run()" :class="{ active: editor.isActive('bulletList') }">• فهرست</button>
      <button type="button" @click="editor.chain().focus().toggleOrderedList().run()" :class="{ active: editor.isActive('orderedList') }">۱. فهرست</button>
      <button type="button" @click="editor.chain().focus().toggleBlockquote().run()" :class="{ active: editor.isActive('blockquote') }">نقل‌قول</button>
      <button type="button" @click="addLink">لینک</button>
      <button type="button" @click="addImage">تصویر</button>
      <button type="button" @click="editor.chain().focus().undo().run()">↺</button>
      <button type="button" @click="editor.chain().focus().redo().run()">↻</button>
    </div>
    <EditorContent :editor="editor" class="rich-content" />
  </div>
</template>

<style>
.rich-editor { border: 1.5px solid var(--c-border, #ddd); border-radius: 8px; overflow: hidden; background: #fff; }
.rich-toolbar { display: flex; flex-wrap: wrap; gap: 4px; padding: 8px; background: #f4f2ec; border-bottom: 1px solid #e3e0d7; }
.rich-toolbar button { border: 1px solid transparent; background: #fff; border-radius: 6px; padding: 4px 10px; cursor: pointer; font-family: inherit; font-size: .9rem; }
.rich-toolbar button:hover { border-color: #ccc; }
.rich-toolbar button.active { background: #2e9e5b; color: #fff; }
.rich-content .ProseMirror { min-height: 280px; padding: 14px; outline: none; line-height: 2; }
.rich-content .ProseMirror:focus { outline: none; }
.rich-content .ProseMirror > * + * { margin-top: .75em; }
.rich-content .ProseMirror ul, .rich-content .ProseMirror ol { padding-inline-start: 1.4rem; }
.rich-content .ProseMirror blockquote { border-inline-start: 3px solid #2e9e5b; padding-inline-start: 1rem; color: #555; }
.rich-content .ProseMirror img { max-width: 100%; height: auto; border-radius: 6px; }
</style>
