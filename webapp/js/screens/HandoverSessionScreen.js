import { setActiveSessionContext } from '../state/appState.js';

function readRuntime(status, keys, fallback = 'Unknown') {
  for (const key of keys) {
    if (status && status[key] !== undefined && status[key] !== null && status[key] !== '') return status[key];
  }
  return fallback;
}

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

function createSummaryCard(label, value) {
  const card = createElement('article', 'session-summary-card');
  card.append(createElement('span', null, label), createElement('strong', null, value));
  return card;
}

function createActionCard(label, route, note) {
  const button = createElement('button', 'session-workflow-card');
  button.dataset.nav = route;
  button.append(createElement('span', null, label), createElement('small', null, note));
  return button;
}

function addReadinessRow(parent, label, value, title) {
  parent.append(createElement('span', null, label));
  const strong = createElement('strong', null, value);
  if (title) strong.title = title;
  parent.append(strong);
}

function statusFromMode(mode) {
  if (mode === 'create') return 'Draft not started';
  if (mode === 'continue') return 'Draft in progress';
  return 'Existing handover lookup';
}

function addNavigateHandlers(root) {
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
  }));
}

export function renderHandoverSessionScreen(root, state, sessionConfig) {
  const dateLabel = sessionConfig.date || new Date().toLocaleDateString();
  const sessionStatus = statusFromMode(sessionConfig.mode);
  setActiveSessionContext({
    selectedShift: sessionConfig.shiftCode,
    selectedShiftLabel: sessionConfig.shiftLabel,
    activeSessionMode: sessionConfig.mode,
    activeSessionDate: dateLabel,
    activeSessionStatus: sessionStatus,
    activeSessionId: state.session?.sessionId || null,
    activeSessionSummary: {
      status: state.session?.sessionStatus || sessionStatus,
      updatedAt: state.session?.updatedAt || null,
      updatedBy: state.session?.updatedBy || state.session?.createdBy || 'Supervisor'
    }
  });

  const runtime = state.runtimeStatus;
  const provider = readRuntime(runtime, ['effectiveProvider', 'EffectiveProvider']);
  const dataRoot = readRuntime(runtime, ['approvedDataRoot', 'ApprovedDataRoot']);
  const lockStatus = readRuntime(runtime, ['appLockStatus', 'AppLockStatus']);
  const canRead = !!readRuntime(runtime, ['appCanRead', 'AppCanRead'], false);
  const canWrite = !!readRuntime(runtime, ['appCanWrite', 'AppCanWrite'], false);
  const sqliteReady = !!readRuntime(runtime, ['sqliteBootstrapSucceeded', 'SqliteBootstrapSucceeded'], false);
  const accessPath = readRuntime(runtime, ['accessDatabasePath', 'AccessDatabasePath'], '');
  const lastSaved = state.session?.updatedAt || state.session?.createdAt || 'Not saved yet';
  const modeLabel = sessionConfig.mode === 'create' ? 'Create' : sessionConfig.mode === 'continue' ? 'Continue' : 'Open';

  const section = createElement('section', `handover-session shift-${sessionConfig.accent}`);

  const header = createElement('div', 'session-header-card');
  header.append(
    createElement('p', 'session-kicker', sessionConfig.shiftLabel),
    createElement('h2', null, `${sessionConfig.shiftLabel} Handover Session`),
    createElement('p', null, `Hours: ${sessionConfig.hours} · Date: ${dateLabel}`),
    createElement('p', null, `Mode: ${modeLabel} · Status: ${sessionStatus}`)
  );
  section.append(header);

  const summaryGrid = createElement('div', 'session-summary-grid');
  summaryGrid.append(
    createSummaryCard('Session date', dateLabel),
    createSummaryCard('Shift', sessionConfig.shiftCode),
    createSummaryCard('Supervisor/User', state.session?.updatedBy || state.session?.createdBy || 'Supervisor'),
    createSummaryCard('Status', sessionStatus),
    createSummaryCard('Last saved', lastSaved),
    createSummaryCard('Data provider', provider)
  );
  section.append(summaryGrid);

  const actionsRow = createElement('div', 'session-actions-row');
  const startLabel = modeLabel === 'Create' ? 'Start / Create Session' : modeLabel === 'Continue' ? 'Continue Session' : 'Open Session Lookup';
  const startButton = createElement('button', 'btn btn-primary', startLabel);
  startButton.dataset.action = 'start';
  actionsRow.append(
    startButton,
    Object.assign(createElement('button', 'btn btn-ghost', 'Save Draft (Phase 10F+)'), { disabled: true }),
    createButton('Back to Shift Dashboard', 'btn btn-ghost', sessionConfig.dashboardRoute),
    createButton('Back to Home', 'btn btn-ghost', 'home')
  );
  section.append(actionsRow);

  const workflowGrid = createElement('div', 'session-workflow-grid');
  workflowGrid.append(
    createActionCard('Department Board', 'departmentBoard', 'Supervisor handover board for this shift/session'),
    createActionCard('Budget', 'budgetMenu', 'Budget UI available via module menu'),
    createActionCard('Attachments', 'attachments', 'Phase 10H full workflow'),
    createActionCard('Preview / Reports', 'reports', 'Preview and reporting module')
  );
  section.append(workflowGrid);

  const readiness = createElement('div', 'session-readiness-panel');
  readiness.append(createElement('h3', null, 'Runtime / Readiness'));
  const readinessGrid = createElement('div', 'session-readiness-grid');
  addReadinessRow(readinessGrid, 'Active provider', provider);
  addReadinessRow(readinessGrid, 'Data root', dataRoot, dataRoot);
  addReadinessRow(readinessGrid, 'Lock status', lockStatus);
  addReadinessRow(readinessGrid, 'Read / Write', `${canRead ? 'Read' : 'Blocked'} / ${canWrite ? 'Write' : 'Read-only'}`);
  addReadinessRow(readinessGrid, 'AccessLegacy', accessPath ? 'Ready' : 'Unavailable');
  addReadinessRow(readinessGrid, 'SQLite readiness', sqliteReady ? 'Ready' : 'Not started');
  readiness.append(readinessGrid);
  section.append(readiness);

  root.replaceChildren(section);

  addNavigateHandlers(root);
  root.querySelector('[data-action="start"]')?.addEventListener('click', () => {
    const nextRoute = sessionConfig.mode === 'open' ? 'history' : 'dashboard';
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: nextRoute } }));
  });
}
