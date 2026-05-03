import { previewService } from '../services/previewService.js';
import { reportsService } from '../services/reportsService.js';
import { applyPreviewPayload, appendGeneratedReport } from '../state/appState.js';

const DEPARTMENT_STATUS_LABELS = new Map([
  ['complete', 'Completed'],
  ['completed', 'Completed'],
  ['incomplete', 'Incomplete'],
  ['not updated', 'Not updated'],
  ['not running', 'Not running']
]);

const READINESS_LABELS = new Map([
  ['ready', 'Ready'],
  ['needs review', 'Needs review'],
  ['missing', 'Missing'],
  ['not available', 'Not available'],
  ['future phase', 'Future phase']
]);

function createElement(tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (text !== undefined && text !== null) element.textContent = String(text);
  return element;
}

function createButton(label, className = 'btn', route = null) {
  const button = createElement('button', className, label);
  button.type = 'button';
  if (route) button.dataset.nav = route;
  return button;
}

function safe(value, fallback = '—') {
  return value === undefined || value === null || value === '' ? fallback : String(value);
}

function formatNumber(value, fallback = 'Not available') {
  const n = Number(value);
  return Number.isFinite(n) ? n.toFixed(0) : fallback;
}

function formatPercent(value) {
  const n = Number(value);
  return Number.isFinite(n) ? `${n.toFixed(1)}%` : 'Not available';
}

function formatDate(value) {
  if (!value) return new Date().toLocaleDateString();
  const text = String(value);
  if (text.includes('-')) {
    const [datePart] = text.split('T');
    const parts = datePart.split('-');
    if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
  }
  return text;
}

function formatDateTime(value) {
  if (!value) return 'Not available';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return safe(value);
  return date.toLocaleString();
}

function clampDepartmentStatus(value) {
  const key = String(value || '').trim().toLowerCase();
  return DEPARTMENT_STATUS_LABELS.get(key) || 'Not updated';
}

function clampReadiness(value) {
  const key = String(value || '').trim().toLowerCase();
  return READINESS_LABELS.get(key) || 'Needs review';
}

function readinessClass(status) {
  const label = clampReadiness(status);
  if (label === 'Ready') return 'value-green';
  if (label === 'Needs review') return 'value-orange';
  if (label === 'Missing') return 'value-red';
  return 'value-muted';
}

function addInfoItem(parent, label, value) {
  const item = createElement('div', 'infobar-item');
  item.append(createElement('span', null, `${label}: ${safe(value)}`));
  parent.append(item);
}

function createFieldRow(label, value, valueClass = '') {
  const row = createElement('div', 'field-row');
  row.append(createElement('span', 'field-label', label), createElement('span', `field-value ${valueClass}`.trim(), safe(value)));
  return row;
}

function createReadinessCard(label, status, detail) {
  const card = createElement('article', 'preview-readiness-card');
  const statusLabel = clampReadiness(status);
  card.append(createElement('span', null, label), createElement('strong', readinessClass(statusLabel), statusLabel));
  if (detail) card.append(createElement('small', null, detail));
  return card;
}

function createSection(title, className = '') {
  const section = createElement('section', `section-block ${className}`.trim());
  section.append(createElement('div', 'section-header', title));
  return section;
}

function navigate(route) {
  window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
}

function getSession(payload, state) {
  return payload?.session || state.session || {};
}

function getDepartments(payload, state) {
  return Array.isArray(payload?.departments) && payload.departments.length
    ? payload.departments
    : Array.isArray(state.session?.departments) ? state.session.departments : [];
}

function getBudgetSummary(payload, state) {
  return payload?.budgetSummary || state.budgetSummary || state.activeBudget?.summary || {};
}

function getBudgetRows(payload, state) {
  return Array.isArray(payload?.budgetRows) && payload.budgetRows.length
    ? payload.budgetRows
    : Array.isArray(state.activeBudget?.rows) ? state.activeBudget.rows : [];
}

function getAttachmentRows(payload, state) {
  if (Array.isArray(payload?.attachments)) return payload.attachments;
  if (Array.isArray(state.activeAttachments) && state.activeAttachments.length) return state.activeAttachments;
  if (Array.isArray(payload?.attachmentSummary)) {
    return payload.attachmentSummary.flatMap((group) => Array.isArray(group.attachments)
      ? group.attachments.map((attachment) => ({ ...attachment, deptName: group.deptName, status: attachment.status || 'Ready' }))
      : []);
  }
  return [];
}

function calculateDepartmentReadiness(departments) {
  if (!departments.length) return ['Missing', 'No department status rows loaded'];
  const needsReview = departments.some((dept) => clampDepartmentStatus(dept.deptStatus) !== 'Completed');
  return needsReview ? ['Needs review', 'Some departments are incomplete or not running'] : ['Ready', 'All departments completed'];
}

function calculateBudgetReadiness(summary, rows) {
  if (!rows.length && !Object.keys(summary || {}).length) return ['Missing', 'No budget summary loaded'];
  const variance = Number(summary?.varianceTotal ?? summary?.variance);
  if (Number.isFinite(variance) && variance !== 0) return ['Needs review', `Variance ${variance > 0 ? '+' : ''}${variance}`];
  return ['Ready', 'Budget totals available'];
}

function calculateAttachmentReadiness(rows) {
  if (!rows.length) return ['Not available', 'No attachments saved or loaded'];
  const needs = rows.some((row) => ['Needs review', 'Missing', 'Not uploaded'].includes(clampReadiness(row.status || 'Ready')));
  return needs ? ['Needs review', 'Some attachments need review'] : ['Ready', 'Attachments available'];
}

function renderDepartmentPreview(container, departments) {
  container.replaceChildren();
  if (!departments.length) {
    container.append(createElement('p', 'status-line warn', 'No saved department status rows were loaded.'));
    return;
  }

  const table = createElement('table', 'preview-table');
  const thead = document.createElement('thead');
  const headRow = document.createElement('tr');
  ['Area', 'Status', 'Notes / Summary', 'Updated'].forEach((label) => headRow.append(createElement('th', null, label)));
  thead.append(headRow);
  const tbody = document.createElement('tbody');

  departments.forEach((dept) => {
    const row = document.createElement('tr');
    row.append(
      createElement('td', null, safe(dept.deptName || dept.department)),
      createElement('td', null, clampDepartmentStatus(dept.deptStatus || dept.status)),
      createElement('td', null, safe(dept.notes || dept.deptNotes, 'No notes recorded')),
      createElement('td', null, `${formatDateTime(dept.updatedAt)}${dept.updatedBy ? ` by ${dept.updatedBy}` : ''}`)
    );
    tbody.append(row);
  });

  table.append(thead, tbody);
  container.append(table);
}

function renderBudgetPreview(container, summary, rows) {
  container.replaceChildren();
  const grid = createElement('div', 'field-list');
  grid.append(
    createFieldRow('Lines planned', formatNumber(summary?.linesPlanned ?? summary?.linesCount)),
    createFieldRow('Total staff required', formatNumber(summary?.plannedTotal ?? summary?.requiredTotal), 'value-orange'),
    createFieldRow('Total staff used', formatNumber(summary?.usedTotal), 'value-green'),
    createFieldRow('Variance', formatNumber(summary?.varianceTotal ?? summary?.variance), readinessClass(Number(summary?.varianceTotal ?? summary?.variance) === 0 ? 'Ready' : 'Needs review')),
    createFieldRow('Holiday', formatNumber(summary?.holidayCount)),
    createFieldRow('Absent', formatNumber(summary?.absentCount)),
    createFieldRow('Agency used', formatNumber(summary?.agencyUsedCount))
  );
  container.append(grid);

  if (!rows.length) {
    container.append(createElement('p', 'status-line warn', 'Budget rows are not loaded in this preview.'));
    return;
  }

  const sample = rows.slice(0, 6);
  const table = createElement('table', 'preview-table');
  const thead = document.createElement('thead');
  const headRow = document.createElement('tr');
  ['Department', 'Budget Staff', 'Staff Used', 'Reason'].forEach((label) => headRow.append(createElement('th', null, label)));
  thead.append(headRow);
  const tbody = document.createElement('tbody');
  sample.forEach((budgetRow) => {
    const row = document.createElement('tr');
    row.append(
      createElement('td', null, safe(budgetRow.deptName || budgetRow.department)),
      createElement('td', null, formatNumber(budgetRow.plannedQty ?? budgetRow.budgetStaff)),
      createElement('td', null, formatNumber(budgetRow.usedQty ?? budgetRow.staffUsed)),
      createElement('td', null, safe(budgetRow.reasonText || budgetRow.reason, ''))
    );
    tbody.append(row);
  });
  table.append(thead, tbody);
  container.append(table);
}

function renderAttachmentPreview(container, rows) {
  container.replaceChildren();
  if (!rows.length) {
    container.append(createElement('p', 'status-line warn', 'No attachments were loaded for this preview.'));
    return;
  }

  const table = createElement('table', 'preview-table');
  const thead = document.createElement('thead');
  const headRow = document.createElement('tr');
  ['Area', 'File name', 'Status', 'Added'].forEach((label) => headRow.append(createElement('th', null, label)));
  thead.append(headRow);
  const tbody = document.createElement('tbody');

  rows.slice(0, 8).forEach((attachment) => {
    const row = document.createElement('tr');
    row.append(
      createElement('td', null, safe(attachment.deptName || attachment.area, 'General Handover')),
      createElement('td', null, safe(attachment.displayName || attachment.fileName, 'Unknown file')),
      createElement('td', null, clampReadiness(attachment.status || 'Ready')),
      createElement('td', null, `${formatDateTime(attachment.addedAt || attachment.createdAt)}${attachment.addedBy ? ` by ${attachment.addedBy}` : ''}`)
    );
    tbody.append(row);
  });

  table.append(thead, tbody);
  container.append(table);
}

function renderOutputPanel(container, reports, messageText) {
  container.replaceChildren();
  if (messageText) container.append(createElement('p', 'status-line', messageText));
  if (!Array.isArray(reports) || !reports.length) {
    container.append(createElement('p', 'status-line warn', 'No real report output has been generated in this session yet.'));
    return;
  }

  reports.forEach((report) => {
    const item = createElement('article', 'preview-report-output');
    item.append(
      createElement('strong', null, safe(report.reportType || report.type, 'report')),
      createElement('span', null, `Generated: ${formatDateTime(report.generatedAt)}${report.generatedBy ? ` by ${report.generatedBy}` : ''}`)
    );
    const paths = Array.isArray(report.filePaths) ? report.filePaths : [];
    if (!paths.length) {
      item.append(createElement('small', null, 'No file paths returned by report service.'));
    } else {
      paths.forEach((path) => item.append(createElement('small', null, path)));
    }
    container.append(item);
  });
}

export function renderPreviewScreen(root, state) {
  const sessionId = state.activeSessionId || state.session?.sessionId || null;
  const screen = createElement('section', 'screen preview-reports-screen');

  const header = createElement('header', 'screen-header');
  const backHeader = createButton('‹', 'header-back', 'departmentBoard');
  const brand = createElement('div', 'header-brand');
  brand.append(createElement('div', 'header-brand-text', 'MOAT HOUSE\nOPERATIONS'));
  header.append(backHeader, brand, createElement('div', 'header-title', 'PREVIEW / REPORTS'));
  const headerActions = createElement('div', 'header-actions');
  headerActions.append(createElement('span', null, state.session?.userName || state.session?.updatedBy || 'Supervisor'));
  header.append(headerActions);
  screen.append(header);

  const info = createElement('div', 'screen-infobar');
  addInfoItem(info, 'Date', formatDate(state.activeSessionDate || state.session?.shiftDate));
  addInfoItem(info, 'Shift', state.selectedShift || state.session?.shiftCode || '—');
  addInfoItem(info, 'Session', sessionId || 'Not opened');
  addInfoItem(info, 'Status', state.activeSessionStatus || state.session?.sessionStatus || 'Open');
  screen.append(info);

  const content = createElement('div', 'screen-content preview-reports-content');
  const message = createElement('p', 'status-line', 'Loading preview data…');
  content.append(message);

  const readiness = createSection('Report readiness', 'preview-readiness-section');
  const readinessGrid = createElement('div', 'preview-readiness-grid');
  readiness.append(readinessGrid);
  content.append(readiness);

  const departmentSection = createSection('Department Status preview', 'preview-departments-section');
  const departmentBody = createElement('div', 'preview-section-body');
  departmentSection.append(departmentBody);
  content.append(departmentSection);

  const budgetSection = createSection('Budget Summary preview', 'preview-budget-section');
  const budgetBody = createElement('div', 'preview-section-body');
  budgetSection.append(budgetBody);
  content.append(budgetSection);

  const attachmentSection = createSection('Attachments preview', 'preview-attachments-section');
  const attachmentBody = createElement('div', 'preview-section-body');
  attachmentSection.append(attachmentBody);
  content.append(attachmentSection);

  const actionSection = createSection('Report actions', 'preview-report-actions-section');
  const actionGrid = createElement('div', 'preview-report-actions-grid');
  const handoverBtn = createButton('Generate Handover Report', 'btn btn-secondary');
  const budgetBtn = createButton('Generate Budget Report', 'btn btn-secondary');
  const attachmentPackBtn = createButton('Generate Attachment Pack / Evidence Pack', 'btn btn-secondary');
  attachmentPackBtn.disabled = true;
  attachmentPackBtn.title = 'Attachment pack generation is a future phase unless host service adds it.';
  const allBtn = createButton('Generate All Reports', 'btn btn-primary');
  const folderBtn = createButton('Open Reports Folder', 'btn btn-secondary');
  const sendBtn = createButton('Continue to Send', 'btn btn-primary');
  sendBtn.disabled = true;
  sendBtn.title = 'Send / Email Review UI comes in Phase 10J.';
  actionGrid.append(handoverBtn, budgetBtn, attachmentPackBtn, allBtn, folderBtn, sendBtn);
  actionSection.append(actionGrid);
  content.append(actionSection);

  const outputSection = createSection('Report output / status', 'preview-output-section');
  const outputBody = createElement('div', 'preview-output-body');
  outputSection.append(outputBody);
  content.append(outputSection);
  screen.append(content);

  const footer = createElement('footer', 'screen-footer');
  const refreshBtn = createButton('Refresh Preview', 'btn btn-secondary');
  const sessionBtn = createButton('Back to Handover Session', 'btn btn-ghost');
  const boardBtn = createButton('Back to Department Status Board', 'btn btn-ghost');
  const budgetBackBtn = createButton('Back to Budget', 'btn btn-ghost');
  const attachmentsBackBtn = createButton('Back to Attachments', 'btn btn-ghost');
  const homeBtn = createButton('Home', 'btn btn-ghost');
  footer.append(refreshBtn, sessionBtn, boardBtn, budgetBackBtn, attachmentsBackBtn, homeBtn);
  screen.append(footer);

  root.replaceChildren(screen);

  let latestPayload = state.preview || null;

  function backToSession() {
    const mode = state.activeSessionMode || 'continue';
    navigate(mode === 'create' ? 'sessionCreate' : mode === 'open' ? 'sessionOpen' : 'sessionContinue');
  }

  function renderPreview(payload, statusMessage = 'Preview loaded from saved state or browser mock data.') {
    latestPayload = payload || latestPayload || {};
    const session = getSession(latestPayload, state);
    const departments = getDepartments(latestPayload, state);
    const budgetSummary = getBudgetSummary(latestPayload, state);
    const budgetRows = getBudgetRows(latestPayload, state);
    const attachmentRows = getAttachmentRows(latestPayload, state);
    const [departmentStatus, departmentDetail] = calculateDepartmentReadiness(departments);
    const [budgetStatus, budgetDetail] = calculateBudgetReadiness(budgetSummary, budgetRows);
    const [attachmentStatus, attachmentDetail] = calculateAttachmentReadiness(attachmentRows);

    readinessGrid.replaceChildren(
      createReadinessCard('Department Status', departmentStatus, departmentDetail),
      createReadinessCard('Budget Summary', budgetStatus, budgetDetail),
      createReadinessCard('Attachments', attachmentStatus, attachmentDetail),
      createReadinessCard('Comments / Notes', departments.some((dept) => dept.notes || dept.deptNotes) ? 'Ready' : 'Needs review', 'Department notes are summarised below'),
      createReadinessCard('Export readiness', session?.sessionId ? 'Ready' : 'Missing', session?.sessionId ? 'Reports can be requested from host service' : 'Open a session first'),
      createReadinessCard('Send readiness', 'Future phase', 'Email review/send workflow is Phase 10J')
    );

    renderDepartmentPreview(departmentBody, departments);
    renderBudgetPreview(budgetBody, budgetSummary, budgetRows);
    renderAttachmentPreview(attachmentBody, attachmentRows);
    renderOutputPanel(outputBody, state.generatedReports || [], 'Report actions only show real service results when returned by the host bridge.');
    message.textContent = statusMessage;
    message.className = 'status-line success';

    addInfoItem(info, 'Last updated', session?.updatedAt || state.session?.updatedAt || 'Not available');
  }

  async function loadPreview() {
    message.textContent = 'Loading preview data from host service…';
    message.className = 'status-line';
    if (!sessionId) {
      renderPreview({ session: state.session, departments: state.session?.departments || [] }, 'No active persisted session id. Showing current app state only.');
      return;
    }
    try {
      const payload = await previewService.loadPreview(sessionId);
      applyPreviewPayload(payload);
      renderPreview(payload, 'Preview loaded from host service.');
    } catch (error) {
      const fallbackPayload = {
        session: state.session,
        departments: state.session?.departments || [],
        budgetSummary: state.budgetSummary || state.activeBudget?.summary || {},
        budgetRows: state.activeBudget?.rows || [],
        attachments: state.activeAttachments || []
      };
      renderPreview(fallbackPayload, error instanceof Error ? `Host preview unavailable. Showing current app state. ${error.message}` : 'Host preview unavailable. Showing current app state.');
    }
  }

  async function runReport(label, runner) {
    message.textContent = `${label} requested…`;
    message.className = 'status-line';
    [handoverBtn, budgetBtn, allBtn, folderBtn].forEach((button) => { button.disabled = true; });
    try {
      const result = await runner();
      appendGeneratedReport(result);
      renderOutputPanel(outputBody, state.generatedReports || [], `${label} completed. Only host-returned files are shown.`);
      message.textContent = `${label} completed.`;
      message.className = 'status-line success';
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : `${label} failed.`;
      message.className = 'status-line error';
    } finally {
      [handoverBtn, budgetBtn, allBtn, folderBtn].forEach((button) => { button.disabled = false; });
    }
  }

  refreshBtn.addEventListener('click', loadPreview);
  sessionBtn.addEventListener('click', backToSession);
  boardBtn.addEventListener('click', () => navigate('departmentBoard'));
  budgetBackBtn.addEventListener('click', () => navigate('budgetMenu'));
  attachmentsBackBtn.addEventListener('click', () => navigate('attachments'));
  homeBtn.addEventListener('click', () => navigate('home'));
  backHeader.addEventListener('click', () => navigate('departmentBoard'));
  handoverBtn.addEventListener('click', () => runReport('Handover report', () => reportsService.generateHandoverReport(sessionId, state.session?.userName || '')));
  budgetBtn.addEventListener('click', () => runReport('Budget report', () => reportsService.generateBudgetReport(sessionId, state.session?.userName || '')));
  allBtn.addEventListener('click', () => runReport('All reports', () => reportsService.generateAllReports(sessionId, state.session?.userName || '')));
  folderBtn.addEventListener('click', () => runReport('Open Reports Folder', () => reportsService.openReportsFolder(sessionId)));

  loadPreview();
}
