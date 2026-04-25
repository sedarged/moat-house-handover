import { departmentsService } from '../services/departmentsService.js';
import {
  applyActiveDepartmentPayload,
  applyDepartmentSummaryPayload,
  setActiveDepartmentName
} from '../state/appState.js';

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function optionMarkup(departments, selectedDeptName) {
  return departments
    .map((dept) => {
      const safeDeptName = escapeHtml(dept.deptName);
      const selected = dept.deptName === selectedDeptName ? 'selected' : '';
      return `<option value="${safeDeptName}" ${selected}>${safeDeptName}</option>`;
    })
    .join('');
}

function metricFieldsMarkup(model) {
  if (!model?.isMetricDept) {
    return '<p class="meta">This is a non-metric department. Downtime, efficiency, and yield are not used.</p>';
  }

  return `
    <label>
      Downtime (min)
      <input type="number" min="0" step="1" name="downtimeMin" value="${escapeHtml(model.downtimeMin ?? '')}" />
    </label>
    <label>
      Efficiency (%)
      <input type="number" min="0" max="100" step="0.1" name="efficiencyPct" value="${escapeHtml(model.efficiencyPct ?? '')}" />
    </label>
    <label>
      Yield (%)
      <input type="number" min="0" max="100" step="0.1" name="yieldPct" value="${escapeHtml(model.yieldPct ?? '')}" />
    </label>
  `;
}

function toNumberOrNull(value) {
  if (value === '' || value == null) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

export function renderDepartmentScreen(root, state) {
  const session = state.session;
  if (!session?.sessionId) {
    root.innerHTML = '<section class="panel"><h2>Department</h2><p class="meta">Open a session first.</p></section>';
    return;
  }

  const deptOptions = session.departments || [];
  const selectedDeptName = state.activeDepartmentName || deptOptions[0]?.deptName;
  if (!selectedDeptName) {
    root.innerHTML = '<section class="panel"><h2>Department</h2><p class="meta">No departments available in this session.</p></section>';
    return;
  }

  setActiveDepartmentName(selectedDeptName);

  root.innerHTML = `
    <section class="panel">
      <h2>Department</h2>
      <p class="meta">Session #${escapeHtml(session.sessionId)} • ${escapeHtml(session.shiftCode)} • ${escapeHtml(session.shiftDate)}</p>
      <label>
        Department
        <select id="dept-selector">${optionMarkup(deptOptions, selectedDeptName)}</select>
      </label>
      <p id="dept-message" class="meta">Loading persisted department values...</p>
      <div id="dept-form-area"></div>
      <div class="actions-row">
        <button id="dept-back" class="secondary" type="button">Back to Dashboard</button>
      </div>
    </section>
  `;

  const selector = root.querySelector('#dept-selector');
  const message = root.querySelector('#dept-message');
  const formArea = root.querySelector('#dept-form-area');

  async function loadSelectedDepartment() {
    const deptName = selector.value;
    setActiveDepartmentName(deptName);
    message.textContent = `Loading ${deptName}...`;

    try {
      const department = await departmentsService.loadDepartment(session.sessionId, deptName);
      applyActiveDepartmentPayload(department);
      renderEditForm(department);
      message.textContent = `Loaded ${deptName}.`;
    } catch (error) {
      formArea.innerHTML = '';
      message.textContent = error instanceof Error ? error.message : 'Failed to load department.';
    }
  }

  function renderEditForm(model) {
    formArea.innerHTML = `
      <form id="dept-form" class="form-grid">
        <label>
          Status
          <input type="text" name="deptStatus" required value="${escapeHtml(model.deptStatus || 'Not running')}" />
        </label>
        ${metricFieldsMarkup(model)}
        <label>
          Notes
          <textarea name="deptNotes" rows="5">${escapeHtml(model.deptNotes || '')}</textarea>
        </label>
        <div class="actions-row">
          <button type="submit">Save Department</button>
          <button id="save-return" type="button" class="secondary">Save + Return</button>
        </div>
      </form>
    `;

    const form = root.querySelector('#dept-form');
    const saveReturn = root.querySelector('#save-return');

    const performSave = async (goDashboard) => {
      const data = new FormData(form);
      const payload = {
        deptRecordId: model.deptRecordId,
        sessionId: session.sessionId,
        deptName: model.deptName,
        deptStatus: String(data.get('deptStatus') || '').trim() || 'Not running',
        deptNotes: String(data.get('deptNotes') || ''),
        downtimeMin: toNumberOrNull(data.get('downtimeMin')),
        efficiencyPct: toNumberOrNull(data.get('efficiencyPct')),
        yieldPct: toNumberOrNull(data.get('yieldPct')),
        userName: session.userName || ''
      };

      const validation = departmentsService.validateDepartment(payload);
      if (!validation.ok) {
        message.textContent = validation.errors.join(' ');
        return;
      }

      message.textContent = `Saving ${payload.deptName}...`;
      try {
        const saveResult = await departmentsService.saveDepartment(payload);
        applyActiveDepartmentPayload(saveResult.department);
        applyDepartmentSummaryPayload(saveResult.dashboardDepartments);
        selector.value = saveResult.department.deptName;
        renderEditForm(saveResult.department);
        message.textContent = `Saved ${payload.deptName} at ${saveResult.department.updatedAt || 'now'}.`;

        if (goDashboard) {
          window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
        }
      } catch (error) {
        message.textContent = error instanceof Error ? error.message : 'Failed to save department.';
      }
    };

    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      await performSave(false);
    });

    saveReturn.addEventListener('click', async () => {
      await performSave(true);
    });
  }

  selector.addEventListener('change', async () => {
    await loadSelectedDepartment();
  });

  root.querySelector('#dept-back')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  void loadSelectedDepartment();
}
