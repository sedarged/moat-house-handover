import { budgetService } from '../services/budgetService.js';
import { applyBudgetPayload, applyBudgetSummaryPayload } from '../state/appState.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar,
  iconClockSm, iconSave, iconRefresh, iconChart
} from '../core/icons.js';

function toNumberOrNull(value) {
  if (value === '' || value == null) return null;
  const n = Number(value);
  return Number.isNaN(n) ? null : n;
}

function fmt(value) {
  if (value == null || value === '') return '—';
  const n = Number(value);
  return Number.isFinite(n) ? n.toFixed(0) : '—';
}

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function varianceClass(v) {
  if (v == null) return '';
  const n = Number(v);
  if (n > 0)  return 'variance-over';
  if (n < 0)  return 'variance-under';
  return 'variance-target';
}

function budgetStatusClass(status) {
  const s = (status || '').toLowerCase();
  if (s === 'over')      return 'value-orange';
  if (s === 'under')     return 'value-green';
  if (s === 'on target') return 'value-green';
  return 'value-muted';
}

function collectMeta(screen) {
  return {
    linesPlanned: toNumberOrNull(screen.querySelector('#sum-lines-input')?.value),
    totalStaffOnRegister: toNumberOrNull(screen.querySelector('#sum-register-input')?.value),
    comments: screen.querySelector('#budget-comments')?.value || ""
  };
}

function collectRows(tableBody) {
  const rows = [];
  tableBody.querySelectorAll('tr[data-row-id]').forEach((tr) => {
    rows.push({
      budgetRowId: Number(tr.dataset.rowId),
      deptName:    tr.dataset.deptName || '',
      plannedQty:  toNumberOrNull(tr.querySelector('input[name="plannedQty"]')?.value),
      usedQty:     toNumberOrNull(tr.querySelector('input[name="usedQty"]')?.value),
      reasonText:  tr.querySelector('textarea[name="reasonText"]')?.value || ''
    });
  });
  return rows;
}

function validateRows(rows) {
  const errors = [];
  rows.forEach((r) => {
    if (r.plannedQty != null && r.plannedQty < 0) errors.push(`${r.deptName || 'Row'}: planned cannot be negative.`);
    if (r.usedQty   != null && r.usedQty   < 0) errors.push(`${r.deptName || 'Row'}: used cannot be negative.`);
  });
  return { ok: errors.length === 0, errors };
}

function validateMeta(meta) {
  const errors = [];
  if (meta.linesPlanned != null && meta.linesPlanned < 0) errors.push('Lines planned cannot be negative.');
  if (meta.totalStaffOnRegister != null && meta.totalStaffOnRegister < 0) errors.push('Total staff on register cannot be negative.');
  return { ok: errors.length === 0, errors };
}

function buildRows(tableBody, rows) {
  tableBody.innerHTML = '';
  rows.forEach((row) => {
    const tr = document.createElement('tr');
    tr.dataset.rowId   = String(row.budgetRowId);
    tr.dataset.deptName = row.deptName || '';

    const deptTd = document.createElement('td');
    deptTd.textContent = row.deptName || '';
    deptTd.className = 'budget-dept-name';

    const plannedTd = document.createElement('td');
    const plannedIn = document.createElement('input');
    plannedIn.type = 'number'; plannedIn.name = 'plannedQty';
    plannedIn.min = '0'; plannedIn.step = '1';
    plannedIn.value = row.plannedQty ?? '';
    plannedIn.placeholder = '0';
    plannedTd.append(plannedIn);

    const usedTd = document.createElement('td');
    const usedIn = document.createElement('input');
    usedIn.type = 'number'; usedIn.name = 'usedQty';
    usedIn.min = '0'; usedIn.step = '1';
    usedIn.value = row.usedQty ?? '';
    usedIn.placeholder = '0';
    usedTd.append(usedIn);

    const varTd = document.createElement('td');
    varTd.className = varianceClass(row.variance);
    varTd.textContent = row.variance != null ? (Number(row.variance) > 0 ? '+' : '') + fmt(row.variance) : '—';

    const reasonTd = document.createElement('td');
    const reasonTa = document.createElement('textarea');
    reasonTa.name = 'reasonText'; reasonTa.rows = 1;
    reasonTa.value = row.reasonText || '';
    reasonTa.placeholder = 'Reason…';
    reasonTd.append(reasonTa);

    tr.append(deptTd, plannedTd, usedTd, varTd, reasonTd);
    tableBody.append(tr);
  });
}

export function renderBudgetScreen(root, state) {
  const session = state.session;
  if (!session?.sessionId) {
    root.innerHTML = `<div class="screen"><div class="screen-content" style="display:flex;align-items:center;justify-content:center;"><p class="status-line">Open a session first.</p></div></div>`;
    return;
  }

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="budget-back-hdr" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">LABOUR BUDGET</div>
      <div class="header-actions">
        <button class="header-icon-btn" type="button">${iconBell}</button>
        <span class="header-divider"></span>
        <button class="header-icon-btn" type="button">${iconUser}</button>
      </div>
    </header>

    <div class="screen-infobar">
      <div class="infobar-item">
        <span class="infobar-icon">${iconCalendar}</span>
        <span>Date: ${formatDate(session.shiftDate)}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClockSm}</span>
        <span>Shift: ${session.shiftCode}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconUser}</span>
        <span>Session #${session.sessionId}</span>
      </div>
      <div class="infobar-item" id="budget-status-badge"></div>
    </div>

    <div class="screen-content">
      <div class="section-block budget-section">
        <div class="section-header">
          <span class="section-icon">${iconChart}</span>
          <span class="section-title">Budget Summary</span>
        </div>
        <div class="budget-summary-grid" id="budget-totals-grid">
          <div><div class="form-label">Lines planned</div><input id="sum-lines-input" type="number" min="0" step="1" placeholder="0" /></div>
          <div><div class="form-label">Total staff required</div><div id="tot-planned" class="budget-summary-value">—</div></div>
          <div><div class="form-label">Total staff used</div><div id="tot-used" class="budget-summary-value">—</div></div>
          <div><div class="form-label">Total staff on register</div><input id="sum-register-input" type="number" min="0" step="1" placeholder="0" /></div>
          <div><div class="form-label">Variance (Used - Required)</div><div id="tot-variance" class="budget-summary-value">—</div></div>
        </div>
        <div class="budget-summary-grid">
          <div><div class="form-label">Holiday count</div><div id="sum-holiday">—</div></div>
          <div><div class="form-label">Absent count</div><div id="sum-absent">—</div></div>
          <div><div class="form-label">Other reason count</div><div id="sum-other">—</div></div>
          <div><div class="form-label">Agency used count</div><div id="sum-agency">—</div></div>
          <div><div class="form-label">Overall Status</div><div id="tot-status" class="budget-summary-value">—</div></div>
        </div>
        <div><label class="form-label" for="budget-comments">Comments</label><textarea id="budget-comments" rows="2" class="budget-comments"></textarea></div>
        <p class="status-line budget-updated-line" id="budget-updated"></p>
      </div>

      <div class="section-block budget-section budget-rows-section">
        <div class="section-header">
          <span class="section-title">Labour Rows</span>
        </div>
        <p class="status-line" id="budget-message">Loading budget…</p>
        <div class="table-wrap budget-table-wrap">
          <table>
            <thead>
              <tr>
                <th class="budget-col-dept">Department</th>
                <th class="budget-col-planned">Budget Staff / Planned Staff</th>
                <th class="budget-col-used">Staff Used</th>
                <th class="budget-col-variance">Variance</th>
                <th class="budget-col-reason">Reason / note</th>
              </tr>
            </thead>
            <tbody id="budget-rows"></tbody>
          </table>
        </div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost" id="budget-back-footer" type="button">${iconArrowLeft}&nbsp; Dashboard</button>
      <button class="btn" id="budget-recalc" type="button">${iconRefresh}&nbsp; Recalculate</button>
      <button class="btn btn-primary" id="budget-save" type="button">${iconSave}&nbsp; Save Budget</button>
    </footer>
  `;

  root.append(screen);

  const message      = screen.querySelector('#budget-message');
  const tableBody    = screen.querySelector('#budget-rows');
  const updatedLine  = screen.querySelector('#budget-updated');
  const statusBadge  = screen.querySelector('#budget-status-badge');
  const totPlanned   = screen.querySelector('#tot-planned');
  const totUsed      = screen.querySelector('#tot-used');
  const totVariance  = screen.querySelector('#tot-variance');
  const totStatus    = screen.querySelector('#tot-status');
  const sumLines = screen.querySelector('#sum-lines-input');
  const sumRegister = screen.querySelector('#sum-register-input');
  const sumHoliday = screen.querySelector('#sum-holiday');
  const sumAbsent = screen.querySelector('#sum-absent');
  const sumOther = screen.querySelector('#sum-other');
  const sumAgency = screen.querySelector('#sum-agency');
  const comments = screen.querySelector('#budget-comments');
  const recalcBtn = screen.querySelector('#budget-recalc');
  const saveBtn = screen.querySelector('#budget-save');

  const goBack = () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  screen.querySelector('#budget-back-hdr')?.addEventListener('click', goBack);
  screen.querySelector('#budget-back-footer')?.addEventListener('click', goBack);

  function renderTotals(totals, summary) {
    const variance = totals?.varianceTotal ?? 0;
    const status   = totals?.status || summary?.status || 'Not set';

    totPlanned.textContent  = fmt(totals?.plannedTotal);
    totUsed.textContent     = fmt(totals?.usedTotal);
    totVariance.textContent = (Number(variance) > 0 ? '+' : '') + fmt(variance);
    totVariance.className   = varianceClass(variance);
    totStatus.textContent   = status;
    sumLines.value = summary?.linesPlanned ?? summary?.linesCount ?? "";
    sumRegister.value = summary?.totalStaffOnRegister ?? "";
    sumHoliday.textContent = fmt(summary?.holidayCount);
    sumAbsent.textContent = fmt(summary?.absentCount);
    sumOther.textContent = fmt(summary?.otherReasonCount);
    sumAgency.textContent = fmt(summary?.agencyUsedCount);
    comments.value = summary?.comments || '';
    totStatus.className     = budgetStatusClass(status);

    statusBadge.textContent = '';
    const badge = document.createElement('span');
    badge.className = 'status-badge budget-status-badge';
    badge.append('Budget: ');
    const strong = document.createElement('strong');
    strong.className = budgetStatusClass(status);
    strong.textContent = status;
    badge.append(strong);
    statusBadge.append(badge);
    updatedLine.textContent = summary?.lastUpdatedAt
      ? `Last saved: ${summary.lastUpdatedAt} by ${summary.lastUpdatedBy || 'n/a'}`
      : '';
  }

  function refreshFromPayload(payload) {
    applyBudgetPayload(payload);
    buildRows(tableBody, payload.rows || []);
    renderTotals(payload.totals, payload.summary);
    applyBudgetSummaryPayload(payload.summary || null);
  }

  async function runRecalculate() {
    const rows = collectRows(tableBody);
    const meta = collectMeta(screen);
    const rowValidation = validateRows(rows);
    const metaValidation = validateMeta(meta);
    if (!rowValidation.ok || !metaValidation.ok) {
      message.textContent = [...rowValidation.errors, ...metaValidation.errors].join(' ');
      message.className = 'status-line error';
      return false;
    }

    message.textContent = 'Recalculating…';
    message.className = 'status-line';
    recalcBtn.disabled = true;
    saveBtn.disabled = true;
    try {
      const payload = await budgetService.recalculate(session.sessionId, rows, meta);
      refreshFromPayload(payload);
      message.textContent = 'Recalculated.';
      message.className = 'status-line success';
      return true;
    } catch (e) {
      message.textContent = e instanceof Error ? e.message : 'Recalculate failed.';
      message.className = 'status-line error';
      return false;
    } finally {
      recalcBtn.disabled = false;
      saveBtn.disabled = false;
    }
  }

  async function loadBudget() {
    message.textContent = 'Loading budget from storage…';
    message.className   = 'status-line';
    try {
      const payload = await budgetService.loadBudget(session.sessionId, session.userName || '');
      refreshFromPayload(payload);
      message.textContent = `Loaded ${payload.rows?.length || 0} row(s).`;
      message.className   = 'status-line success';
    } catch (e) {
      message.textContent = e instanceof Error ? e.message : 'Failed to load budget.';
      message.className   = 'status-line error';
    }
  }

  screen.querySelector('#budget-recalc')?.addEventListener('click', runRecalculate);

  screen.querySelector('#budget-save')?.addEventListener('click', async () => {
    const rows = collectRows(tableBody);
    const meta = collectMeta(screen);
    const rowValidation = validateRows(rows);
    const metaValidation = validateMeta(meta);
    if (!rowValidation.ok || !metaValidation.ok) {
      message.textContent = [...rowValidation.errors, ...metaValidation.errors].join(' ');
      message.className   = 'status-line error';
      return;
    }
    message.textContent = 'Saving budget…';
    message.className   = 'status-line';
    recalcBtn.disabled = true;
    saveBtn.disabled = true;
    try {
      const payload = await budgetService.saveBudget(session.sessionId, rows, meta, session.userName || '');
      refreshFromPayload(payload);
      message.textContent = 'Budget saved.';
      message.className   = 'status-line success';
    } catch (e) {
      message.textContent = e instanceof Error ? e.message : 'Save failed.';
      message.className   = 'status-line error';
    } finally {
      recalcBtn.disabled = false;
      saveBtn.disabled = false;
    }
  });

  tableBody.addEventListener('input', (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;
    if (target.matches('input[name="plannedQty"], input[name="usedQty"], textarea[name="reasonText"]')) {
      message.textContent = 'Unsaved changes. Recalculate to refresh totals.';
      message.className = 'status-line warn';
    }
  });

  sumLines?.addEventListener('input', () => {
    message.textContent = 'Unsaved changes. Recalculate to refresh totals.';
    message.className = 'status-line warn';
  });
  sumRegister?.addEventListener('input', () => {
    message.textContent = 'Unsaved changes. Recalculate to refresh totals.';
    message.className = 'status-line warn';
  });

  loadBudget();
}
