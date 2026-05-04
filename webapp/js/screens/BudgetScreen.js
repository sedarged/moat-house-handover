import { budgetService } from '../services/budgetService.js';
import { applyBudgetPayload, applyBudgetSummaryPayload } from '../state/appState.js';

const FALLBACK_BUDGET_ROWS = [
  { budgetRowId: 1, deptName: 'Injection', plannedQty: 6, usedQty: 6, reasonText: '' },
  { budgetRowId: 2, deptName: 'MetaPress', plannedQty: 4, usedQty: 4, reasonText: '' },
  { budgetRowId: 3, deptName: 'Berks', plannedQty: 3, usedQty: 3, reasonText: '' },
  { budgetRowId: 4, deptName: 'Wilts', plannedQty: 3, usedQty: 2, reasonText: 'Holiday' },
  { budgetRowId: 5, deptName: 'Further Processing', plannedQty: 4, usedQty: 3, reasonText: 'Absent' },
  { budgetRowId: 6, deptName: 'Brine operative', plannedQty: 2, usedQty: 2, reasonText: '' },
  { budgetRowId: 7, deptName: 'Rack cleaner / domestic', plannedQty: 2, usedQty: 1, reasonText: 'Holiday' },
  { budgetRowId: 8, deptName: 'Goods In', plannedQty: 2, usedQty: 2, reasonText: '' },
  { budgetRowId: 9, deptName: 'Dry Goods', plannedQty: 2, usedQty: 1, reasonText: 'Agency cover' },
  { budgetRowId: 10, deptName: 'Supervisors', plannedQty: 3, usedQty: 3, reasonText: '' },
  { budgetRowId: 11, deptName: 'Admin', plannedQty: 2, usedQty: 2, reasonText: '' },
  { budgetRowId: 12, deptName: 'Cleaners', plannedQty: 2, usedQty: 2, reasonText: '' },
  { budgetRowId: 13, deptName: 'Stock controller', plannedQty: 1, usedQty: 1, reasonText: '' },
  { budgetRowId: 14, deptName: 'Training', plannedQty: 1, usedQty: 1, reasonText: '' },
  { budgetRowId: 15, deptName: 'Trolley Porter T1/T2', plannedQty: 2, usedQty: 2, reasonText: '' },
  { budgetRowId: 16, deptName: 'Butchery', plannedQty: 2, usedQty: 2, reasonText: '' }
];

function createElement(tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (text !== undefined && text !== null) element.textContent = String(text);
  return element;
}

function createButton(label, className, id) {
  const button = createElement('button', className, label);
  button.type = 'button';
  if (id) button.id = id;
  return button;
}

function createInfoItem(label, value) {
  const item = createElement('div', 'infobar-item');
  item.append(createElement('span', null, `${label}: ${value}`));
  return item;
}

function createFieldCard(label, value, className = 'budget-summary-value') {
  const card = createElement('div');
  card.append(createElement('div', 'form-label', label), createElement('div', className, value));
  return card;
}

function createSummaryLine(label, value, valueClass = '') {
  const row = createElement('div', 'field-row');
  row.append(createElement('span', 'field-label', label), createElement('span', `field-value ${valueClass}`.trim(), value));
  return row;
}

function toNumberOrNull(value) {
  if (value === '' || value == null) return null;
  const n = Number(value);
  return Number.isNaN(n) ? null : n;
}

function asNumber(value) {
  const n = Number(value);
  return Number.isFinite(n) ? n : 0;
}

function fmt(value) {
  if (value == null || value === '') return '—';
  const n = Number(value);
  return Number.isFinite(n) ? n.toFixed(0) : '—';
}

function formatDate(value) {
  if (!value) return new Date().toLocaleDateString();
  if (String(value).includes('-')) {
    const parts = String(value).split('T')[0].split('-');
    if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return String(value);
}

function formatTime(value) {
  if (!value) return 'Not updated';
  const text = String(value);
  if (text.includes('T')) return text.split('T')[1]?.slice(0, 5) || text;
  return text;
}

function clampReason(value) {
  const reason = String(value || '').trim();
  if (!reason) return '';
  const lower = reason.toLowerCase();
  if (lower.includes('holiday')) return 'Holiday';
  if (lower.includes('absent')) return 'Absent';
  if (lower.includes('agency')) return 'Agency cover';
  if (lower.includes('other')) return 'Other reason';
  return reason;
}

function varianceClass(value) {
  const n = Number(value);
  if (!Number.isFinite(n)) return 'value-muted';
  if (n < 0) return 'value-red';
  if (n > 0) return 'value-orange';
  return 'value-green';
}

function normalizeRows(rows) {
  const source = Array.isArray(rows) && rows.length ? rows : FALLBACK_BUDGET_ROWS;
  return source.map((row, index) => {
    const plannedQty = toNumberOrNull(row.plannedQty ?? row.budgetStaff ?? row.plannedStaff);
    const usedQty = toNumberOrNull(row.usedQty ?? row.staffUsed);
    const variance = asNumber(usedQty) - asNumber(plannedQty);
    return {
      budgetRowId: Number(row.budgetRowId ?? index + 1),
      deptName: String(row.deptName || row.department || row.labourArea || FALLBACK_BUDGET_ROWS[index]?.deptName || `Row ${index + 1}`),
      plannedQty,
      usedQty,
      variance,
      reasonText: clampReason(row.reasonText ?? row.reason ?? '')
    };
  });
}

function deriveSummary(rows, meta = {}, existingSummary = {}) {
  const plannedTotal = rows.reduce((sum, row) => sum + asNumber(row.plannedQty), 0);
  const usedTotal = rows.reduce((sum, row) => sum + asNumber(row.usedQty), 0);
  const varianceTotal = usedTotal - plannedTotal;
  const reasons = rows.map((row) => String(row.reasonText || '').toLowerCase());
  return {
    linesPlanned: toNumberOrNull(meta.linesPlanned ?? existingSummary.linesPlanned ?? existingSummary.linesCount) ?? 3,
    totalStaffOnRegister: toNumberOrNull(meta.totalStaffOnRegister ?? existingSummary.totalStaffOnRegister) ?? 44,
    plannedTotal,
    usedTotal,
    varianceTotal,
    holidayCount: reasons.filter((reason) => reason.includes('holiday')).length,
    absentCount: reasons.filter((reason) => reason.includes('absent')).length,
    agencyUsedCount: reasons.filter((reason) => reason.includes('agency')).length,
    otherReasonCount: reasons.filter((reason) => reason && !reason.includes('holiday') && !reason.includes('absent') && !reason.includes('agency')).length,
    comments: meta.comments ?? existingSummary.comments ?? 'One operator moved to cover line support.\nNo agency required.',
    lastUpdatedAt: existingSummary.lastUpdatedAt || existingSummary.updatedAt || null,
    status: varianceTotal === 0 ? 'On target' : varianceTotal < 0 ? 'Under' : 'Over'
  };
}

function collectRows(tableBody) {
  return [...tableBody.querySelectorAll('tr[data-row-id]')].map((tr) => ({
    budgetRowId: Number(tr.dataset.rowId),
    deptName: tr.dataset.deptName || '',
    plannedQty: toNumberOrNull(tr.querySelector('input[name="plannedQty"]')?.value),
    usedQty: toNumberOrNull(tr.querySelector('input[name="usedQty"]')?.value),
    reasonText: clampReason(tr.querySelector('input[name="reasonText"]')?.value || '')
  }));
}

function collectMeta(screen) {
  return {
    linesPlanned: toNumberOrNull(screen.querySelector('#sum-lines-input')?.value),
    totalStaffOnRegister: toNumberOrNull(screen.querySelector('#sum-register-input')?.value),
    comments: screen.querySelector('#budget-comments')?.value || ''
  };
}

function validateRows(rows) {
  const errors = [];
  rows.forEach((row) => {
    if (row.plannedQty != null && row.plannedQty < 0) errors.push(`${row.deptName || 'Row'}: planned cannot be negative.`);
    if (row.usedQty != null && row.usedQty < 0) errors.push(`${row.deptName || 'Row'}: used cannot be negative.`);
  });
  return errors;
}

function validateMeta(meta) {
  const errors = [];
  if (meta.linesPlanned != null && meta.linesPlanned < 0) errors.push('Lines planned cannot be negative.');
  if (meta.totalStaffOnRegister != null && meta.totalStaffOnRegister < 0) errors.push('Total staff on register cannot be negative.');
  return errors;
}

function addNavigate(button, route) {
  button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
  });
}

function buildHeader(screen, session, dateLabel, shiftCode, linesPlanned, lastUpdated) {
  const header = createElement('header', 'screen-header');
  const back = createButton('‹', 'header-back', 'budget-back-hdr');
  const brand = createElement('div', 'header-brand');
  brand.append(createElement('div', 'header-brand-text', 'MOAT HOUSE\nOPERATIONS'));
  header.append(back, brand, createElement('div', 'header-title', 'BUDGET SUMMARY'));
  const actions = createElement('div', 'header-actions');
  actions.append(createElement('span', null, session?.userName || 'Supervisor'));
  header.append(actions);
  screen.append(header);

  const info = createElement('div', 'screen-infobar');
  info.append(
    createInfoItem('Date', dateLabel),
    createInfoItem('Shift', shiftCode),
    createInfoItem('Lines planned', linesPlanned),
    createInfoItem('Last updated', lastUpdated)
  );
  screen.append(info);
}

function buildTableRows(tableBody, rows, editable) {
  tableBody.replaceChildren();
  rows.forEach((row) => {
    const tr = document.createElement('tr');
    tr.dataset.rowId = String(row.budgetRowId);
    tr.dataset.deptName = row.deptName;
    tr.append(createElement('td', 'budget-dept-name', row.deptName));

    const plannedTd = document.createElement('td');
    const plannedInput = document.createElement('input');
    plannedInput.type = 'number';
    plannedInput.name = 'plannedQty';
    plannedInput.min = '0';
    plannedInput.step = '1';
    plannedInput.value = row.plannedQty ?? '';
    plannedInput.disabled = !editable;
    plannedTd.append(plannedInput);
    tr.append(plannedTd);

    const usedTd = document.createElement('td');
    const usedInput = document.createElement('input');
    usedInput.type = 'number';
    usedInput.name = 'usedQty';
    usedInput.min = '0';
    usedInput.step = '1';
    usedInput.value = row.usedQty ?? '';
    usedInput.disabled = !editable;
    usedTd.append(usedInput);
    tr.append(usedTd);

    const reasonTd = document.createElement('td');
    const reasonInput = document.createElement('input');
    reasonInput.type = 'text';
    reasonInput.name = 'reasonText';
    reasonInput.value = row.reasonText || '';
    reasonInput.placeholder = 'Reason';
    reasonInput.disabled = !editable;
    reasonTd.append(reasonInput);
    tr.append(reasonTd);
    tableBody.append(tr);
  });

  const totals = deriveSummary(rows);
  const totalRow = document.createElement('tr');
  totalRow.className = 'budget-total-row';
  totalRow.append(
    createElement('td', 'budget-dept-name', 'Total number of staff'),
    createElement('td', null, fmt(totals.plannedTotal)),
    createElement('td', null, fmt(totals.usedTotal)),
    createElement('td', varianceClass(totals.varianceTotal), totals.varianceTotal > 0 ? `+${fmt(totals.varianceTotal)}` : fmt(totals.varianceTotal))
  );
  tableBody.append(totalRow);
}

export function renderBudgetScreen(root, state) {
  const session = state.session || {};
  const activeSessionId = state.activeSessionId ?? session.sessionId ?? null;
  const shiftCode = state.selectedShift || session.shiftCode || 'PM';
  const dateLabel = formatDate(state.activeSessionDate || session.shiftDate);
  let editable = false;
  let currentRows = normalizeRows(state.activeBudget?.rows || state.budgetRows);
  let currentSummary = deriveSummary(currentRows, {}, state.budgetSummary || {});

  const screen = createElement('div', 'screen shift-budget');
  buildHeader(screen, session, dateLabel, shiftCode, currentSummary.linesPlanned, formatTime(currentSummary.lastUpdatedAt));

  const content = createElement('div', 'screen-content two-col');
  const left = createElement('div');
  const right = createElement('aside');

  const tableBlock = createElement('section', 'section-block budget-section budget-rows-section');
  tableBlock.append(createElement('div', 'section-header', 'Department labour budget'));
  const message = createElement('p', 'status-line', 'Budget persistence is not wired in this environment.');
  message.id = 'budget-message';
  tableBlock.append(message);
  const tableWrap = createElement('div', 'table-wrap budget-table-wrap');
  const table = document.createElement('table');
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  ['Department', 'Budget Staff', 'Staff Used', 'Reason'].forEach((label) => headerRow.append(createElement('th', null, label)));
  thead.append(headerRow);
  const tableBody = document.createElement('tbody');
  tableBody.id = 'budget-rows';
  table.append(thead, tableBody);
  tableWrap.append(table);
  tableBlock.append(tableWrap);
  left.append(tableBlock);

  const summaryPanel = createElement('section', 'section-block budget-section');
  summaryPanel.append(createElement('div', 'section-header', 'Summary'));
  const summaryGrid = createElement('div', 'field-list');
  summaryPanel.append(summaryGrid);
  right.append(summaryPanel);

  const commentsPanel = createElement('section', 'section-block budget-section');
  commentsPanel.append(createElement('div', 'section-header', 'Comments'));
  const comments = document.createElement('textarea');
  comments.id = 'budget-comments';
  comments.className = 'budget-comments';
  comments.disabled = true;
  commentsPanel.append(comments);
  right.append(commentsPanel);

  content.append(left, right);
  screen.append(content);

  const footer = createElement('footer', 'screen-footer');
  const refreshBtn = createButton('Refresh', 'btn', 'budget-refresh');
  const editBtn = createButton('Edit', 'btn', 'budget-edit');
  const saveBtn = createButton('Save & Close', 'btn btn-primary', 'budget-save');
  const printBtn = createButton('Print', 'btn', 'budget-print');
  footer.append(refreshBtn, editBtn, saveBtn, printBtn);
  screen.append(footer);

  root.replaceChildren(screen);

  function renderState(statusMessage = '', messageClass = 'warn') {
    currentRows = normalizeRows(currentRows);
    currentSummary = deriveSummary(currentRows, collectMeta(screen), currentSummary);
    buildTableRows(tableBody, currentRows, editable);
    summaryGrid.replaceChildren(
      createSummaryLine('Date', dateLabel),
      createSummaryLine('Shift', shiftCode),
      createSummaryLine('Lines planned', fmt(currentSummary.linesPlanned)),
      createSummaryLine('Total staff required', fmt(currentSummary.plannedTotal), 'value-orange'),
      createSummaryLine('Total number of staff used', fmt(currentSummary.usedTotal), 'value-green'),
      createSummaryLine('Total staff on register', fmt(currentSummary.totalStaffOnRegister), 'value-orange'),
      createSummaryLine('Holiday', fmt(currentSummary.holidayCount), 'value-orange'),
      createSummaryLine('Absent', fmt(currentSummary.absentCount), 'value-orange'),
      createSummaryLine('Other reason', fmt(currentSummary.otherReasonCount)),
      createSummaryLine('Agency used', fmt(currentSummary.agencyUsedCount)),
      createSummaryLine('Variance', currentSummary.varianceTotal > 0 ? `+${fmt(currentSummary.varianceTotal)}` : fmt(currentSummary.varianceTotal), varianceClass(currentSummary.varianceTotal))
    );
    comments.value = currentSummary.comments || '';
    comments.disabled = !editable;
    message.textContent = statusMessage || 'Budget persistence is not wired in this environment.';
    message.className = `status-line ${messageClass}`;
    editBtn.textContent = editable ? 'View' : 'Edit';
  }

  function recalculateFromInputs() {
    const rows = collectRows(tableBody);
    const meta = collectMeta(screen);
    const errors = [...validateRows(rows), ...validateMeta(meta)];
    if (errors.length) {
      message.textContent = errors.join(' ');
      message.className = 'status-line error';
      return false;
    }
    currentRows = normalizeRows(rows);
    currentSummary = deriveSummary(currentRows, meta, currentSummary);
    return true;
  }

  async function loadBudget() {
    if (!activeSessionId) {
      renderState('No active handover session. Create, continue, or open a handover first.', 'warn');
      return;
    }
    message.textContent = 'Loading budget from host service…';
    message.className = 'status-line';
    try {
      const payload = await budgetService.loadBudget(activeSessionId, session.userName || '');
      currentRows = normalizeRows(payload.rows);
      currentSummary = deriveSummary(currentRows, {}, payload.summary || {});
      applyBudgetPayload(payload);
      applyBudgetSummaryPayload(currentSummary);
      renderState('Budget loaded from host.', 'success');
    } catch (error) {
      renderState(error instanceof Error ? `Budget persistence is not wired in this environment. ${error.message}` : 'Budget persistence is not wired in this environment.', 'warn');
    }
  }

  refreshBtn.addEventListener('click', loadBudget);
  editBtn.addEventListener('click', () => {
    editable = !editable;
    renderState(editable ? 'Edit mode enabled. Save & Close validates rows before host save.' : 'View mode enabled.');
  });
  saveBtn.addEventListener('click', async () => {
    if (!recalculateFromInputs()) return;
    if (!activeSessionId) {
      renderState('No active handover session. Create, continue, or open a handover first.', 'warn');
      return;
    }
    try {
      const editedRows = normalizeRows(currentRows);
      const editedMeta = collectMeta(screen);
      const payload = await budgetService.saveBudget(activeSessionId, editedRows, editedMeta, session.userName || '');
      const payloadRows = Array.isArray(payload?.rows) && payload.rows.length ? payload.rows : editedRows;
      currentRows = normalizeRows(payloadRows);
      currentSummary = deriveSummary(currentRows, editedMeta, payload?.summary || {});
      applyBudgetPayload({ ...(payload || {}), rows: currentRows, summary: currentSummary });
      applyBudgetSummaryPayload(currentSummary);
      editable = false;
      renderState('Budget saved.', 'success');
    } catch (error) {
      renderState(
        error instanceof Error
          ? `Budget save is not wired in this environment. No data was written. ${error.message}`
          : 'Budget save is not wired in this environment. No data was written.',
        'error'
      );
    }
  });
  printBtn.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'reports' } }));
  });
  screen.querySelector('#budget-back-hdr')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'departmentBoard' } }));
  });

  renderState();
  loadBudget();
}
