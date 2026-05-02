import { applyActiveDepartmentPayload } from '../state/appState.js';

const HANDOVER_AREAS = [
  'Injection',
  'MetaPress',
  'Berks',
  'Wilts',
  'Racking',
  'Butchery',
  'Further Processing',
  'Tumblers',
  'Smoke Tumbler',
  'Minimums & Samples',
  'Goods In & Despatch',
  'Dry Goods',
  'Additional'
];

const FALLBACK_STATUS = {
  Injection: 'Completed',
  MetaPress: 'Incomplete',
  Berks: 'Incomplete',
  Wilts: 'Completed',
  Racking: 'Completed',
  Butchery: 'Not updated',
  'Further Processing': 'Incomplete',
  Tumblers: 'Not running',
  'Smoke Tumbler': 'Not running',
  'Minimums & Samples': 'Not updated',
  'Goods In & Despatch': 'Completed',
  'Dry Goods': 'Incomplete',
  Additional: 'Not updated'
};

const STATUS_LABELS = new Map([
  ['complete', 'Completed'],
  ['completed', 'Completed'],
  ['incomplete', 'Incomplete'],
  ['not updated', 'Not updated'],
  ['not running', 'Not running']
]);

function createElement(tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (text !== undefined && text !== null) element.textContent = String(text);
  return element;
}

function createButton(label, className, route) {
  const button = createElement('button', className, label);
  if (route) button.dataset.nav = route;
  return button;
}

function normaliseStatus(status) {
  const key = String(status || '').trim().toLowerCase();
  return STATUS_LABELS.get(key) || 'Not updated';
}

function statusClass(status) {
  const label = normaliseStatus(status);
  if (label === 'Completed') return 'completed';
  if (label === 'Incomplete') return 'incomplete';
  if (label === 'Not running') return 'not-running';
  return 'not-updated';
}

function metricAverage(items, key) {
  const values = items.map((item) => item[key]).filter((value) => typeof value === 'number');
  if (!values.length) return 'Not available (UI fallback)';
  return `${(values.reduce((sum, value) => sum + value, 0) / values.length).toFixed(1)}%`;
}

function totalDowntime(items) {
  const values = items.map((item) => item.downtimeMin).filter((value) => typeof value === 'number');
  if (!values.length) return '0 min (UI fallback)';
  return `${values.reduce((sum, value) => sum + value, 0)} min`;
}

function sessionBackRoute(mode) {
  if (mode === 'create') return 'sessionCreate';
  if (mode === 'open') return 'sessionOpen';
  return 'sessionContinue';
}

function addNavigateHandlers(root) {
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
  }));
}

function addInfoItem(parent, label, value) {
  const item = createElement('span');
  item.append(document.createTextNode(`${label}: `), createElement('strong', null, value));
  parent.append(item);
}

function addSummaryCard(parent, label, value) {
  const card = createElement('article');
  card.append(createElement('span', null, label), createElement('strong', null, value));
  parent.append(card);
}

export function renderDepartmentStatusBoardScreen(root, state, boardConfig) {
  const date = boardConfig.sessionDate || state.activeSessionDate || new Date().toLocaleDateString();
  const shiftCode = boardConfig.shiftCode || state.selectedShift || 'AM';
  const shiftLabel = boardConfig.shiftLabel || (shiftCode === 'NS' ? 'Night Shift' : `${shiftCode} Shift`);
  const shiftTitle = shiftCode === 'NS' ? 'NIGHT SHIFT HANDOVER' : `${shiftCode} SHIFT HANDOVER`;
  const accent = boardConfig.accent || (shiftCode === 'NS' ? 'ns' : shiftCode.toLowerCase());
  const mode = boardConfig.sessionMode || state.activeSessionMode || 'continue';
  const status = boardConfig.sessionStatus || state.activeSessionStatus || 'Draft in progress';

  const byName = new Map((state.session?.departments || []).map((department) => [department.deptName, department]));
  const boardItems = HANDOVER_AREAS.map((deptName) => {
    const found = byName.get(deptName);
    return {
      deptName,
      deptStatus: normaliseStatus(found?.deptStatus || FALLBACK_STATUS[deptName]),
      updatedAt: found?.updatedAt || null,
      attachmentCount: found?.attachmentCount || 0,
      downtimeMin: found?.downtimeMin,
      efficiencyPct: found?.efficiencyPct,
      yieldPct: found?.yieldPct
    };
  });

  const completed = boardItems.filter((item) => item.deptStatus === 'Completed').length;
  const updated = boardItems.filter((item) => ['Completed', 'Incomplete'].includes(item.deptStatus)).length;
  const lastUpdated = boardItems.map((item) => item.updatedAt).filter(Boolean).sort().reverse()[0] || 'Not updated';
  const attachments = boardItems.reduce((sum, item) => sum + (Number(item.attachmentCount) || 0), 0);

  const section = createElement('section', `department-board shift-${accent}`);

  const header = createElement('div', 'department-board-header');
  const titleBlock = createElement('div');
  titleBlock.append(
    createElement('p', 'department-board-kicker', shiftLabel),
    createElement('h2', null, shiftTitle),
    createElement('p', null, `${mode} · ${status}`)
  );

  const nav = createElement('div', 'department-board-nav');
  nav.append(
    createButton('Back to Handover Session', 'btn btn-ghost', sessionBackRoute(mode)),
    createButton('Shift Dashboard', 'btn btn-ghost', shiftCode === 'PM' ? 'pm' : shiftCode === 'NS' ? 'night' : 'am'),
    createButton('Home', 'btn btn-ghost', 'home')
  );
  header.append(titleBlock, nav);
  section.append(header);

  const infoStrip = createElement('div', 'department-board-info-strip');
  addInfoItem(infoStrip, 'Date', date);
  addInfoItem(infoStrip, 'Shift', shiftCode);
  addInfoItem(infoStrip, 'Updated', `${updated} / ${boardItems.length}`);
  addInfoItem(infoStrip, 'Last updated', lastUpdated);
  section.append(infoStrip);

  const statusSection = createElement('section', 'department-board-section');
  statusSection.append(createElement('h3', null, 'DEPARTMENT STATUS'));
  const grid = createElement('div', 'department-board-grid');
  boardItems.forEach((item) => {
    const card = createElement('article', 'department-card');
    card.append(createElement('h4', null, item.deptName), createElement('span', `status-pill ${statusClass(item.deptStatus)}`, item.deptStatus));
    const openButton = createElement('button', 'btn btn-secondary', 'Open Department');
    openButton.dataset.dept = item.deptName;
    openButton.dataset.status = item.deptStatus;
    card.append(openButton);
    grid.append(card);
  });
  statusSection.append(grid);
  section.append(statusSection);

  const summary = createElement('section', 'department-board-summary');
  summary.append(createElement('h3', null, 'SHIFT SUMMARY'));
  const summaryGrid = createElement('div', 'department-board-summary-grid');
  addSummaryCard(summaryGrid, 'Departments Completed', `${completed} / ${boardItems.length}`);
  addSummaryCard(summaryGrid, 'Total Efficiency', metricAverage(boardItems, 'efficiencyPct'));
  addSummaryCard(summaryGrid, 'Total Yield', metricAverage(boardItems, 'yieldPct'));
  addSummaryCard(summaryGrid, 'Total Downtime', totalDowntime(boardItems));
  addSummaryCard(summaryGrid, 'Attachments count', attachments);
  addSummaryCard(summaryGrid, 'Budget status', 'Draft');
  summary.append(summaryGrid);
  section.append(summary);

  const actions = createElement('div', 'department-board-actions');
  actions.append(
    Object.assign(createElement('button', 'btn btn-primary', 'Save'), { disabled: true, title: 'Save workflow comes next' }),
    createButton('Preview', 'btn btn-secondary', 'reports'),
    Object.assign(createElement('button', 'btn btn-primary', 'Save & Send'), { disabled: true, title: 'Send workflow remains legacy/future' }),
    createButton('Budget', 'btn btn-secondary', 'budgetMenu')
  );
  section.append(actions);

  root.replaceChildren(section);

  addNavigateHandlers(root);
  root.querySelectorAll('[data-dept]').forEach((button) => button.addEventListener('click', () => {
    const item = boardItems.find((candidate) => candidate.deptName === button.dataset.dept) || {
      deptName: button.dataset.dept,
      deptStatus: button.dataset.status || 'Not updated'
    };
    applyActiveDepartmentPayload(item);
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'departmentDetailEntry' } }));
  }));
}
