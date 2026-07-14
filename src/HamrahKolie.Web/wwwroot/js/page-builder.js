(function () {
  'use strict';
  const root = document.getElementById('pb-editor');
  if (!root) return;
  const pageKey = root.dataset.pageKey;
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
  const canvas = document.getElementById('pb-canvas');
  const shell = document.getElementById('pb-canvas-shell');
  const layers = document.getElementById('pb-layers');
  const state = document.getElementById('pb-save-state');
  const undoButton = document.getElementById('pb-undo');
  const redoButton = document.getElementById('pb-redo');
  let selectedId = Number(root.dataset.selectedId || 0) || null;
  let draggedLayer = null;
  let autosaveTimer = null;
  const undoStack = [];
  const redoStack = [];
  const metrics = (function () { try { return JSON.parse(root.dataset.metrics || '[]'); } catch (_) { return []; } })();

  function setStatus(text, mode) {
    state.textContent = text;
    state.className = 'pb-save-state' + (mode ? ' ' + mode : '');
  }
  function headers(json) {
    const h = { 'RequestVerificationToken': token, 'X-PageBuilder': '1' };
    if (json) h['Content-Type'] = 'application/json';
    return h;
  }
  async function responseJson(response) {
    const data = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(data.message || 'عملیات انجام نشد.');
    return data;
  }
  function currentOrder() {
    return Array.from(layers?.querySelectorAll('[data-section-id]') || []).map(x => Number(x.dataset.sectionId));
  }
  function updateHistoryButtons() {
    undoButton.disabled = undoStack.length === 0;
    redoButton.disabled = redoStack.length === 0;
  }
  async function persistOrder(order, addHistory) {
    if (addHistory) {
      const previous = currentOrder();
      if (previous.join(',') !== order.join(',')) { undoStack.push(previous); redoStack.length = 0; }
    }
    setStatus('در حال ذخیره ترتیب…', 'saving');
    const response = await fetch(root.dataset.reorderUrl, { method: 'POST', headers: headers(true), body: JSON.stringify({ pageKey, ids: order }) });
    await responseJson(response);
    setStatus('ترتیب ذخیره شد');
    updateHistoryButtons();
    canvas.contentWindow.location.reload();
  }
  function applyOrder(order) {
    order.forEach(id => { const row = layers.querySelector(`[data-section-id="${id}"]`); if (row) layers.appendChild(row); });
  }

  function selectSection(id) {
    selectedId = Number(id);
    document.querySelectorAll('.pb-layer').forEach(x => x.classList.toggle('active', Number(x.dataset.sectionId) === selectedId));
    document.querySelectorAll('.pb-inspector').forEach(x => x.classList.toggle('active', Number(x.dataset.inspectorId) === selectedId));
    document.getElementById('pb-no-selection').classList.toggle('active', !selectedId);
    try {
      canvas.contentDocument.querySelectorAll('[data-pb-section-id]').forEach(x => x.classList.toggle('pb-editor-selected', Number(x.dataset.pbSectionId) === selectedId));
    } catch (_) { }
  }

  async function createSection(type, beforeId) {
    setStatus('در حال افزودن ابزار…', 'saving');
    const body = new URLSearchParams({ type: String(type), pageKey });
    if (beforeId) body.set('beforeId', String(beforeId));
    body.set('__RequestVerificationToken', token);
    try {
      const data = await responseJson(await fetch(root.dataset.createUrl, { method: 'POST', headers: headers(false), body }));
      location.href = `${location.pathname}?pageKey=${encodeURIComponent(pageKey)}&selected=${data.id}`;
    } catch (error) { setStatus(error.message, 'error'); }
  }

  async function sectionAction(url, id) {
    const body = new URLSearchParams({ id: String(id), pageKey, __RequestVerificationToken: token });
    return responseJson(await fetch(url, { method: 'POST', headers: headers(false), body }));
  }

  function enhanceCanvas() {
    const doc = canvas.contentDocument;
    if (!doc) return;
    const style = doc.createElement('style');
    style.textContent = '[data-pb-section-id]:not([data-pb-section-id=""]){position:relative;outline:1px dashed transparent;outline-offset:-2px;cursor:pointer}[data-pb-section-id]:not([data-pb-section-id=""]):hover{outline-color:#2e9e5b}.pb-editor-selected{outline:3px solid #2e9e5b!important;outline-offset:-3px!important}.pb-editor-selected:before{content:"در حال ویرایش";position:absolute;z-index:9999;top:0;right:0;background:#2e9e5b;color:#fff;padding:3px 8px;font:12px Vazirmatn;border-radius:0 0 0 6px}a{pointer-events:none}';
    doc.head.appendChild(style);
    doc.querySelectorAll('[data-pb-section-id]:not([data-pb-section-id=""])').forEach(node => {
      const id = Number(node.dataset.pbSectionId);
      node.addEventListener('click', event => { event.preventDefault(); event.stopPropagation(); selectSection(id); });
      if (!node.parentElement?.closest('[data-pb-section-id]')) {
        node.draggable = true;
        node.addEventListener('dragstart', event => { event.dataTransfer.setData('application/x-pb-section', String(id)); });
        node.addEventListener('dragover', event => event.preventDefault());
        node.addEventListener('drop', event => {
          event.preventDefault();
          const widgetType = event.dataTransfer.getData('application/x-pb-widget');
          const movedId = Number(event.dataTransfer.getData('application/x-pb-section'));
          if (widgetType) return createSection(widgetType, id);
          if (movedId && movedId !== id) {
            const order = currentOrder().filter(x => x !== movedId);
            order.splice(order.indexOf(id), 0, movedId);
            const previous = currentOrder();
            applyOrder(order); undoStack.push(previous); redoStack.length = 0; persistOrder(order, false).catch(e => setStatus(e.message, 'error'));
          }
        });
      }
    });
    doc.body.addEventListener('dragover', event => event.preventDefault());
    doc.body.addEventListener('drop', event => {
      if (event.target.closest('[data-pb-section-id]')) return;
      const widgetType = event.dataTransfer.getData('application/x-pb-widget');
      if (widgetType) { event.preventDefault(); createSection(widgetType); }
    });
    if (selectedId) selectSection(selectedId);
  }

  document.querySelectorAll('[data-left-tab]').forEach(button => button.addEventListener('click', () => {
    document.querySelectorAll('[data-left-tab]').forEach(x => x.classList.toggle('active', x === button));
    document.querySelectorAll('[data-left-panel]').forEach(x => x.classList.toggle('active', x.dataset.leftPanel === button.dataset.leftTab));
  }));
  document.querySelectorAll('.pb-widget').forEach(widget => {
    widget.addEventListener('click', () => createSection(widget.dataset.widgetType));
    widget.addEventListener('dragstart', event => event.dataTransfer.setData('application/x-pb-widget', widget.dataset.widgetType));
  });
  document.getElementById('pb-widget-search')?.addEventListener('input', event => {
    const query = event.target.value.trim().toLowerCase();
    document.querySelectorAll('.pb-widget').forEach(x => x.hidden = !x.dataset.widgetName.toLowerCase().includes(query));
  });
  document.getElementById('pb-page-select')?.addEventListener('change', event => location.href = `${location.pathname}?pageKey=${encodeURIComponent(event.target.value)}`);
  document.querySelectorAll('[data-select-section]').forEach(x => x.addEventListener('click', () => selectSection(x.dataset.selectSection)));

  layers?.querySelectorAll('.pb-layer').forEach(row => {
    row.addEventListener('dragstart', event => { draggedLayer = row; row.classList.add('dragging'); event.dataTransfer.setData('application/x-pb-section', row.dataset.sectionId); });
    row.addEventListener('dragend', () => { row.classList.remove('dragging'); draggedLayer = null; });
    row.addEventListener('dragover', event => event.preventDefault());
    row.addEventListener('drop', event => {
      event.preventDefault();
      if (!draggedLayer || draggedLayer === row) return;
      const previous = currentOrder();
      const box = row.getBoundingClientRect();
      row.parentElement.insertBefore(draggedLayer, event.clientY > box.top + box.height / 2 ? row.nextSibling : row);
      undoStack.push(previous); redoStack.length = 0;
      persistOrder(currentOrder(), false).catch(e => setStatus(e.message, 'error'));
    });
  });

  document.querySelectorAll('.pb-inspector').forEach(form => {
    form.querySelectorAll('[data-inspector-tab]').forEach(tab => tab.addEventListener('click', () => {
      form.querySelectorAll('[data-inspector-tab]').forEach(x => x.classList.toggle('active', x === tab));
      form.querySelectorAll('[data-inspector-panel]').forEach(x => x.classList.toggle('active', x.dataset.inspectorPanel === tab.dataset.inspectorTab));
    }));
    form.querySelectorAll('input[type="range"]').forEach(range => range.addEventListener('input', () => { const output = range.closest('.pb-range')?.querySelector('output'); if (output) output.textContent = range.value; }));
    form.querySelectorAll('[data-color-target]').forEach(color => color.addEventListener('input', () => { const input = form.querySelector(`[name="${color.dataset.colorTarget}"]`); input.value = color.value; input.dispatchEvent(new Event('input', { bubbles: true })); }));
    form.addEventListener('input', () => {
      setStatus('تغییر ذخیره نشده', 'saving');
      if (!document.getElementById('pb-autosave').checked) return;
      clearTimeout(autosaveTimer); autosaveTimer = setTimeout(() => saveForm(form), 1200);
    });
    form.addEventListener('submit', event => { event.preventDefault(); saveForm(form); });
    hydrateRepeater(form);
  });

  async function saveForm(form) {
    clearTimeout(autosaveTimer);
    serializeRepeater(form);
    setStatus('در حال ذخیره…', 'saving');
    try {
      await responseJson(await fetch(form.action, { method: 'POST', headers: headers(false), body: new FormData(form) }));
      setStatus('همه تغییرات ذخیره شده');
      canvas.contentWindow.location.reload();
    } catch (error) { setStatus(error.message, 'error'); }
  }

  function hydrateRepeater(form) {
    const repeater = form.querySelector('.pb-repeater');
    if (!repeater) return;
    const source = form.querySelector('.pb-settings-json');
    let settings = {}; try { settings = JSON.parse(source.value || '{}'); } catch (_) { }
    const key = repeater.dataset.itemsKey;
    const rows = Array.isArray(settings[key]) ? settings[key] : [];
    rows.forEach(item => addRepeaterRow(repeater, item));
    repeater.querySelector('[data-add-item]').addEventListener('click', () => addRepeaterRow(repeater, {}));
  }
  function addRepeaterRow(repeater, item) {
    const isStats = repeater.dataset.itemsKind === 'stats';
    const row = document.createElement('div'); row.className = 'pb-repeat-row'; row.draggable = true;
    let metricPicker = '';
    if (isStats && metrics.length) {
      const opts = ['<option value="">— داده زنده (اختیاری) —</option>']
        .concat(metrics.map(m => `<option value="${escapeHtml(m.key)}">${escapeHtml(m.label)} (${escapeHtml(m.value)})</option>`))
        .join('');
      metricPicker = `<select class="pb-metric-picker" title="اتصال به داده زنده سایت">${opts}</select>`;
    }
    row.innerHTML = `<span class="pb-repeat-row__drag">⠿</span><div class="pb-repeat-row__fields"><input data-item-field="${isStats ? 'value' : 'title'}" value="${escapeHtml(item[isStats ? 'value' : 'title'] || '')}" placeholder="${isStats ? 'مقدار یا {{data}}' : 'عنوان'}"><input data-item-field="${isStats ? 'label' : 'text'}" value="${escapeHtml(item[isStats ? 'label' : 'text'] || '')}" placeholder="${isStats ? 'برچسب' : 'متن'}">${metricPicker}</div><button type="button" title="حذف">×</button>`;
    const picker = row.querySelector('.pb-metric-picker');
    if (picker) {
      picker.addEventListener('change', () => {
        if (!picker.value) return;
        const valueInput = row.querySelector('[data-item-field="value"]');
        valueInput.value = '{{' + picker.value + '}}';
        const labelInput = row.querySelector('[data-item-field="label"]');
        const chosen = metrics.find(m => m.key === picker.value);
        if (labelInput && !labelInput.value && chosen) labelInput.value = chosen.label;
        repeater.dispatchEvent(new Event('input', { bubbles: true }));
        picker.value = '';
      });
    }
    row.querySelector('button').addEventListener('click', () => { row.remove(); repeater.dispatchEvent(new Event('input', { bubbles:true })); });
    row.addEventListener('dragstart', () => row.classList.add('dragging'));
    row.addEventListener('dragend', () => row.classList.remove('dragging'));
    row.addEventListener('dragover', event => event.preventDefault());
    row.addEventListener('drop', event => {
      event.preventDefault();
      const moving = repeater.querySelector('.dragging');
      if (moving && moving !== row) {
        row.parentElement.insertBefore(moving, row);
        repeater.dispatchEvent(new Event('input', { bubbles:true }));
      }
    });
    repeater.querySelector('.pb-repeater__rows').appendChild(row);
  }
  function serializeRepeater(form) {
    const repeater = form.querySelector('.pb-repeater'); if (!repeater) return;
    const source = form.querySelector('.pb-settings-json');
    let settings = {}; try { settings = JSON.parse(source.value || '{}'); } catch (_) { }
    const key = repeater.dataset.itemsKey;
    settings[key] = Array.from(repeater.querySelectorAll('.pb-repeat-row')).map(row => {
      const item = {}; row.querySelectorAll('[data-item-field]').forEach(input => item[input.dataset.itemField] = input.value); return item;
    });
    source.value = JSON.stringify(settings);
  }
  function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[c])); }

  document.querySelectorAll('[data-delete-section]').forEach(button => button.addEventListener('click', async () => {
    if (!confirm('این سکشن حذف شود؟')) return;
    try { await sectionAction(root.dataset.deleteUrl, button.dataset.deleteSection); location.href = `${location.pathname}?pageKey=${encodeURIComponent(pageKey)}`; }
    catch (error) { setStatus(error.message, 'error'); }
  }));
  document.querySelectorAll('[data-duplicate-section]').forEach(button => button.addEventListener('click', async () => {
    try { const data = await sectionAction(root.dataset.duplicateUrl, button.dataset.duplicateSection); location.href = `${location.pathname}?pageKey=${encodeURIComponent(pageKey)}&selected=${data.id}`; }
    catch (error) { setStatus(error.message, 'error'); }
  }));
  document.getElementById('pb-publish')?.addEventListener('click', async () => {
    setStatus('در حال انتشار…', 'saving');
    const body = new URLSearchParams({ pageKey, __RequestVerificationToken: token });
    try { const data = await responseJson(await fetch(root.dataset.publishUrl, { method:'POST', headers:headers(false), body })); setStatus(`${data.count} سکشن منتشر شد`); canvas.contentWindow.location.reload(); }
    catch (error) { setStatus(error.message, 'error'); }
  });
  document.querySelectorAll('.pb-device').forEach(button => button.addEventListener('click', () => {
    document.querySelectorAll('.pb-device').forEach(x => x.classList.toggle('active', x === button));
    shell.dataset.device = button.dataset.device;
    document.getElementById('pb-viewport-label').textContent = button.dataset.device === 'desktop' ? 'عرض کامل' : button.dataset.device === 'tablet' ? '۷۶۸ پیکسل' : '۳۹۰ پیکسل';
  }));
  undoButton.addEventListener('click', async () => { if (!undoStack.length) return; const now=currentOrder(), order=undoStack.pop(); redoStack.push(now); applyOrder(order); await persistOrder(order,false); });
  redoButton.addEventListener('click', async () => { if (!redoStack.length) return; const now=currentOrder(), order=redoStack.pop(); undoStack.push(now); applyOrder(order); await persistOrder(order,false); });
  document.addEventListener('keydown', event => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase()==='s') { event.preventDefault(); const form=document.querySelector('.pb-inspector.active'); if(form) saveForm(form); } });
  canvas.addEventListener('load', enhanceCanvas);
  if (selectedId) selectSection(selectedId);
})();
