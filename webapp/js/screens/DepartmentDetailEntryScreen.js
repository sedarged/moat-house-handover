import { applyActiveDepartmentPayload, setSelectedAttachmentId } from '../state/appState.js';
import { departmentsService } from '../services/departmentsService.js';
import { attachmentsService } from '../services/attachmentsService.js';

const ALLOWED_STATUSES = ['Completed', 'Incomplete', 'Not updated', 'Not running'];
const METRIC_DEPARTMENTS = new Set(['Injection', 'MetaPress', 'Berks', 'Wilts']);

function createElement(tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (text !== undefined && text !== null) element.textContent = String(text);
  return element;
}

function normaliseStatus(status) {
  const clean = String(status || '').trim().toLowerCase();
  if (clean === 'completed' || clean === 'complete') return 'Completed';
  if (clean === 'incomplete') return 'Incomplete';
  if (clean === 'not running') return 'Not running';
  return 'Not updated';
}

function toNumberOrNull(value) {
  const trimmed = String(value ?? '').trim();
  if (!trimmed) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : NaN;
}

function statusClass(status) {
  const label = normaliseStatus(status);
  if (label === 'Completed') return 'completed';
  if (label === 'Incomplete') return 'incomplete';
  if (label === 'Not running') return 'not-running';
  return 'not-updated';
}

function metricDept(deptName) {
  return METRIC_DEPARTMENTS.has(String(deptName || ''));
}

function bindDataNav(root) {
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }))));
}

export function renderDepartmentDetailEntryScreen(root, state) {
  const source = state.activeDepartment || null;
  const deptName = String(source?.deptName || state.activeDepartmentName || '').trim();
  const hasDepartment = Boolean(deptName);
  const shift = state.selectedShift || state.session?.shiftCode || 'AM';
  const date = state.activeSessionDate || state.session?.shiftDate || new Date().toLocaleDateString();
  const sessionId = state.activeSessionId || state.session?.sessionId || 'Not available';
  const sessionStatus = state.activeSessionStatus || state.session?.sessionStatus || 'Draft';

  const section = createElement('section', 'department-detail-entry department-detail-editor');
  const header = createElement('div', 'department-board-header');
  const titleBlock = createElement('div');
  titleBlock.append(
    createElement('p', 'department-board-kicker', 'Department Data Entry'),
    createElement('h2', null, 'DEPARTMENT DETAIL'),
    createElement('p', null, hasDepartment ? deptName : 'No department selected')
  );
  const nav = createElement('div', 'department-board-nav');
  [['Back to Department Status Board', 'departmentBoard'], ['Back to Handover Session', 'sessionContinue'], ['Home', 'home']].forEach(([label, route]) => {
    const b = createElement('button', 'btn btn-ghost', label); b.dataset.nav = route; nav.append(b);
  });
  header.append(titleBlock, nav);
  section.append(header);

  const info = createElement('div', 'department-board-info-strip');
  [['Date', date], ['Shift', shift], ['Session', String(sessionId)], ['Session status', sessionStatus]].forEach(([l, v]) => {
    const item = createElement('span'); item.append(document.createTextNode(`${l}: `), createElement('strong', null, v)); info.append(item);
  });
  section.append(info);

  if (!hasDepartment) {
    section.append(createElement('p', 'status-line warn', 'No department selected. Return to Department Status Board.'));
    root.replaceChildren(section);
    bindDataNav(root);
    return;
  }

  const initial = {
    deptRecordId: source?.deptRecordId || null,
    sessionId: state.session?.sessionId || null,
    deptName,
    deptStatus: normaliseStatus(source?.deptStatus),
    deptNotes: String(source?.deptNotes || source?.notes || ''),
    issues: String(source?.issues || ''),
    actionsRequired: String(source?.actionsRequired || ''),
    updatedBy: String(source?.updatedBy || state.session?.updatedBy || state.session?.userName || 'Not available'),
    lastUpdated: String(source?.updatedAt || state.session?.updatedAt || 'Not available'),
    downtimeMin: source?.downtimeMin ?? null,
    efficiencyPct: source?.efficiencyPct ?? null,
    yieldPct: source?.yieldPct ?? null
  };

  const draft = { ...initial };
  const isMetric = metricDept(deptName);
  let downtimeInput = null;
  let efficiencyInput = null;
  let yieldInput = null;

  const form = createElement('section', 'department-board-section');
  form.append(createElement('h3', null, 'Department detail editor'));

  const grid = createElement('div', 'two-col');
  const left = createElement('div', 'form-grid');
  const right = createElement('div', 'form-grid');

  function addField(parent, label, el) {
    const wrap = createElement('label', 'form-field'); wrap.append(createElement('span', 'form-label', label), el); parent.append(wrap);
  }

  const statusSelect = createElement('select');
  ALLOWED_STATUSES.forEach((status) => { const option = createElement('option', null, status); option.value = status; statusSelect.append(option); });
  statusSelect.value = draft.deptStatus;
  const statusPill = createElement('span', `status-pill ${statusClass(draft.deptStatus)}`, draft.deptStatus);
  statusSelect.addEventListener('change', () => { draft.deptStatus = normaliseStatus(statusSelect.value); statusPill.className = `status-pill ${statusClass(draft.deptStatus)}`; statusPill.textContent = draft.deptStatus; });
  addField(left, 'Department status', statusSelect);
  left.append(statusPill);

  const notes = createElement('textarea'); notes.value = draft.deptNotes; notes.rows = 4; notes.addEventListener('input', () => { draft.deptNotes = notes.value; });
  addField(left, 'Handover notes', notes);
  const issues = createElement('textarea'); issues.value = draft.issues; issues.rows = 3; issues.addEventListener('input', () => { draft.issues = issues.value; });
  addField(left, 'Issues / blockers', issues);
  const actions = createElement('textarea'); actions.value = draft.actionsRequired; actions.rows = 3; actions.addEventListener('input', () => { draft.actionsRequired = actions.value; });
  addField(left, 'Actions required', actions);

  const updatedBy = createElement('input'); updatedBy.value = draft.updatedBy; updatedBy.addEventListener('input', () => { draft.updatedBy = updatedBy.value; });
  addField(right, 'Updated by', updatedBy);
  const lastUpdated = createElement('input'); lastUpdated.value = draft.lastUpdated; lastUpdated.disabled = true;
  addField(right, 'Last updated', lastUpdated);

  if (isMetric) {
    const down = createElement('input'); down.type = 'number'; down.min = '0'; down.value = draft.downtimeMin ?? ''; down.addEventListener('input', () => { draft.downtimeMin = toNumberOrNull(down.value); });
    addField(right, 'Downtime minutes', down);
    downtimeInput = down;
    const eff = createElement('input'); eff.type = 'number'; eff.min = '0'; eff.max = '100'; eff.value = draft.efficiencyPct ?? ''; eff.addEventListener('input', () => { draft.efficiencyPct = toNumberOrNull(eff.value); });
    addField(right, 'Efficiency %', eff);
    efficiencyInput = eff;
    const yld = createElement('input'); yld.type = 'number'; yld.min = '0'; yld.max = '100'; yld.value = draft.yieldPct ?? ''; yld.addEventListener('input', () => { draft.yieldPct = toNumberOrNull(yld.value); });
    addField(right, 'Yield %', yld);
    yieldInput = yld;
  }

  grid.append(left, right);
  form.append(grid);
  section.append(form);

  const attachmentCard = createElement('section', 'department-board-summary');
  attachmentCard.append(createElement('h3', null, 'Attachment summary'));
  const attachmentLine = createElement('p', 'status-line', 'Attachment data not loaded.');
  attachmentCard.append(attachmentLine);
  section.append(attachmentCard);

  const validationPanel = createElement('section', 'department-board-summary');
  validationPanel.append(createElement('h3', null, 'Validation / save status'));
  const saveLine = createElement('p', 'status-line warn', 'Save is not wired for this environment. No data was written.');
  const errList = createElement('ul');
  validationPanel.append(saveLine, errList);
  section.append(validationPanel);

  const actionsBar = createElement('div', 'department-board-actions');
  const saveBtn = createElement('button', 'btn btn-primary', 'Save Department');
  const resetBtn = createElement('button', 'btn btn-secondary', 'Reset changes');
  const openAttachBtn = createElement('button', 'btn btn-secondary', 'Open Attachments');
  const previewBtn = createElement('button', 'btn btn-secondary', 'Preview / Reports');
  const backBtn = createElement('button', 'btn btn-secondary', 'Back to Department Status Board');
  [previewBtn, backBtn].forEach((button, i) => { button.dataset.nav = i === 0 ? 'reports' : 'departmentBoard'; });
  actionsBar.append(saveBtn, resetBtn, openAttachBtn, previewBtn, backBtn);
  section.append(actionsBar);

  function validate() {
    const payload = {
      ...draft,
      deptStatus: normaliseStatus(draft.deptStatus),
      deptNotes: String(draft.deptNotes || ''),
      downtimeMin: isMetric ? draft.downtimeMin : null,
      efficiencyPct: isMetric ? draft.efficiencyPct : null,
      yieldPct: isMetric ? draft.yieldPct : null
    };
    const errors = [];
    if (!ALLOWED_STATUSES.includes(payload.deptStatus)) errors.push('Status must be one of Completed, Incomplete, Not updated, Not running.');
    if (payload.downtimeMin != null && (Number.isNaN(payload.downtimeMin) || payload.downtimeMin < 0)) errors.push('Downtime minutes must be blank or a non-negative number.');
    if (payload.efficiencyPct != null && (Number.isNaN(payload.efficiencyPct) || payload.efficiencyPct < 0 || payload.efficiencyPct > 100)) errors.push('Efficiency % must be blank or between 0 and 100.');
    if (payload.yieldPct != null && (Number.isNaN(payload.yieldPct) || payload.yieldPct < 0 || payload.yieldPct > 100)) errors.push('Yield % must be blank or between 0 and 100.');
    const serviceValidation = departmentsService.validateDepartment(payload);
    if (!serviceValidation.ok) errors.push(...serviceValidation.errors);
    return { payload, errors };
  }

  saveBtn.addEventListener('click', async () => {
    const { payload, errors } = validate();
    errList.replaceChildren(...errors.map((e) => { const li = createElement('li', 'status-line error', e); return li; }));
    if (errors.length) { saveLine.className = 'status-line error'; saveLine.textContent = 'Fix validation issues before saving.'; return; }
    try {
      const result = await departmentsService.saveDepartment({ ...payload, userName: payload.updatedBy });
      saveLine.className = 'status-line success'; saveLine.textContent = 'Department saved.';
      applyActiveDepartmentPayload({ ...payload, ...result, updatedAt: result?.updatedAt || new Date().toISOString(), updatedBy: result?.updatedBy || payload.updatedBy });
    } catch (_error) {
      saveLine.className = 'status-line warn'; saveLine.textContent = 'Save is not wired for this environment. No data was written.';
    }
  });

  resetBtn.addEventListener('click', () => {
    Object.assign(draft, initial);
    statusSelect.value = initial.deptStatus;
    statusPill.className = `status-pill ${statusClass(initial.deptStatus)}`;
    statusPill.textContent = initial.deptStatus;
    notes.value = initial.deptNotes;
    issues.value = initial.issues;
    actions.value = initial.actionsRequired;
    updatedBy.value = initial.updatedBy;
    lastUpdated.value = initial.lastUpdated;
    if (downtimeInput) downtimeInput.value = initial.downtimeMin ?? '';
    if (efficiencyInput) efficiencyInput.value = initial.efficiencyPct ?? '';
    if (yieldInput) yieldInput.value = initial.yieldPct ?? '';
    errList.replaceChildren();
    saveLine.className = 'status-line'; saveLine.textContent = 'Changes reset to last loaded values.';
  });

  openAttachBtn.addEventListener('click', () => {
    state.activeDepartmentName = deptName;
    setSelectedAttachmentId(null);
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'attachments' } }));
  });

  root.replaceChildren(section);
  bindDataNav(root);

  attachmentsService.listAttachments(state.session?.sessionId || null, source?.deptRecordId || null, deptName)
    .then((result) => {
      const list = Array.isArray(result?.attachments) ? result.attachments : [];
      const needsReview = list.filter((item) => String(item?.status || '').toLowerCase() === 'needs review').length;
      const lastAdded = list.map((item) => item?.addedAt).filter(Boolean).sort().reverse()[0] || 'Not available';
      attachmentLine.textContent = `Attachment count: ${list.length} · Needs review: ${needsReview} · Last attachment: ${lastAdded}`;
    })
    .catch(() => { attachmentLine.textContent = 'Attachment data not loaded.'; });
}
