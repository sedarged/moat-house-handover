import { budgetService } from '../services/budgetService.js';
import { applyBudgetPayload, applyBudgetSummaryPayload } from '../state/appState.js';

function toNumberOrNull(value) {
  if (value === '' || value == null) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function formatNumber(value) {
  const n = Number(value || 0);
  return Number.isFinite(n) ? n.toFixed(2) : '0.00';
}

function createCellInput(type, value, name, options = {}) {
  const input = document.createElement('input');
  input.type = type;
  input.name = name;
  input.value = value ?? '';
  if (options.min != null) {
    input.min = String(options.min);
  }
  if (options.step != null) {
    input.step = String(options.step);
  }
  if (options.rows != null) {
    input.rows = options.rows;
  }

  return input;
}

function collectRowsFromTable(tableBody) {
  const rows = [];
  const trList = tableBody.querySelectorAll('tr[data-row-id]');
  trList.forEach((tr) => {
    rows.push({
      budgetRowId: Number(tr.dataset.rowId),
      deptName: tr.dataset.deptName || '',
      plannedQty: toNumberOrNull(tr.querySelector('input[name="plannedQty"]')?.value),
      usedQty: toNumberOrNull(tr.querySelector('input[name="usedQty"]')?.value),
      reasonText: tr.querySelector('textarea[name="reasonText"]')?.value || ''
    });
  });

  return rows;
}

function validateRows(rows) {
  const errors = [];
  rows.forEach((row) => {
    if (!row.deptName?.trim()) {
      errors.push('Every budget row must have a department name.');
    }

    if (row.plannedQty != null && row.plannedQty < 0) {
      errors.push(`${row.deptName || 'Row'} planned value cannot be negative.`);
    }

    if (row.usedQty != null && row.usedQty < 0) {
      errors.push(`${row.deptName || 'Row'} used value cannot be negative.`);
    }
  });

  return { ok: errors.length === 0, errors };
}

function renderTotals(summaryLine, totals) {
  summaryLine.textContent = `Planned: ${formatNumber(totals.plannedTotal)} • Used: ${formatNumber(totals.usedTotal)} • Variance: ${formatNumber(totals.varianceTotal)} • Status: ${totals.status || 'not set'}`;
}

function createBudgetTableRows(tableBody, rows) {
  tableBody.textContent = '';

  rows.forEach((row) => {
    const tr = document.createElement('tr');
    tr.dataset.rowId = String(row.budgetRowId);
    tr.dataset.deptName = row.deptName || '';

    const deptTd = document.createElement('td');
    deptTd.textContent = row.deptName || '';

    const plannedTd = document.createElement('td');
    plannedTd.append(createCellInput('number', row.plannedQty ?? '', 'plannedQty', { min: 0, step: 0.01 }));

    const usedTd = document.createElement('td');
    usedTd.append(createCellInput('number', row.usedQty ?? '', 'usedQty', { min: 0, step: 0.01 }));

    const varianceTd = document.createElement('td');
    varianceTd.className = 'meta';
    varianceTd.textContent = formatNumber(row.variance);

    const statusTd = document.createElement('td');
    statusTd.className = 'meta';
    statusTd.textContent = row.status || 'not set';

    const reasonTd = document.createElement('td');
    const reasonText = document.createElement('textarea');
    reasonText.name = 'reasonText';
    reasonText.rows = 2;
    reasonText.value = row.reasonText || '';
    reasonTd.append(reasonText);

    tr.append(deptTd, plannedTd, usedTd, varianceTd, statusTd, reasonTd);
    tableBody.append(tr);
  });
}

export function renderBudgetScreen(root, state) {
  const session = state.session;
  if (!session?.sessionId) {
    root.innerHTML = '<section class="panel"><h2>Budget</h2><p class="meta">Open a session first.</p></section>';
    return;
  }

  root.innerHTML = `
    <section class="panel">
      <h2>Budget</h2>
      <p id="budget-session" class="meta"></p>
      <p id="budget-message" class="meta">Loading budget from persisted storage...</p>
      <div class="table-wrap">
        <table class="budget-table">
          <thead>
            <tr>
              <th>Department</th>
              <th>Planned</th>
              <th>Used</th>
              <th>Variance</th>
              <th>Status</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody id="budget-rows"></tbody>
        </table>
      </div>
      <p id="budget-totals" class="meta"></p>
      <p id="budget-updated" class="meta"></p>
      <div class="actions-row">
        <button id="budget-recalc" type="button" class="secondary">Recalculate</button>
        <button id="budget-save" type="button">Save Budget</button>
        <button id="budget-back" type="button" class="secondary">Return to Dashboard</button>
      </div>
    </section>
  `;

  const sessionLine = root.querySelector('#budget-session');
  const message = root.querySelector('#budget-message');
  const tableBody = root.querySelector('#budget-rows');
  const totalsLine = root.querySelector('#budget-totals');
  const updatedLine = root.querySelector('#budget-updated');
  const recalcButton = root.querySelector('#budget-recalc');
  const saveButton = root.querySelector('#budget-save');
  const backButton = root.querySelector('#budget-back');

  sessionLine.textContent = `Session #${session.sessionId} • ${session.shiftCode} • ${session.shiftDate}`;

  function refreshFromPayload(payload) {
    applyBudgetPayload(payload);
    createBudgetTableRows(tableBody, payload.rows || []);
    renderTotals(totalsLine, payload.totals || { plannedTotal: 0, usedTotal: 0, varianceTotal: 0, status: 'not set' });
    updatedLine.textContent = `Last updated: ${payload.summary?.lastUpdatedAt || payload.updatedAt || 'n/a'} by ${payload.summary?.lastUpdatedBy || payload.updatedBy || 'n/a'}`;
  }

  async function loadBudget() {
    try {
      const payload = await budgetService.loadBudget(session.sessionId, session.userName || '');
      refreshFromPayload(payload);
      applyBudgetSummaryPayload(payload.summary || null);
      message.textContent = `Loaded ${payload.rows?.length || 0} budget row(s).`;
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to load budget.';
    }
  }

  recalcButton?.addEventListener('click', async () => {
    const rows = collectRowsFromTable(tableBody);
    const validation = validateRows(rows);
    if (!validation.ok) {
      message.textContent = validation.errors.join(' ');
      return;
    }

    try {
      message.textContent = 'Recalculating totals...';
      const payload = await budgetService.recalculate(session.sessionId, rows);
      refreshFromPayload(payload);
      message.textContent = 'Recalculated totals from current values.';
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to recalculate budget.';
    }
  });

  saveButton?.addEventListener('click', async () => {
    const rows = collectRowsFromTable(tableBody);
    const validation = validateRows(rows);
    if (!validation.ok) {
      message.textContent = validation.errors.join(' ');
      return;
    }

    try {
      message.textContent = 'Saving budget rows...';
      const payload = await budgetService.saveBudget(session.sessionId, rows, session.userName || '');
      refreshFromPayload(payload);
      applyBudgetSummaryPayload(payload.summary || null);
      message.textContent = 'Budget saved to Access-backed storage.';
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to save budget.';
    }
  });

  backButton?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  loadBudget();
}
