import { applySessionPayload, setActiveSessionContext } from '../state/appState.js';
import { sessionService } from '../services/sessionService.js';

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

function normaliseDateForService(value) {
  const text = String(value || '').trim();
  if (!text) return new Date().toISOString().slice(0, 10);
  if (text.includes('-')) return text.split('T')[0];
  const parts = text.split('/');
  if (parts.length === 3) {
    const [day, month, year] = parts;
    return `${year.padStart(4, '20')}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
  }
  return text;
}

function sessionStatusMessage(mode, payload) {
  if (mode === 'create') return `Session created. Active session: ${payload?.sessionId || 'host returned no id'}.`;
  if (mode === 'continue') return `Existing draft loaded. Active session: ${payload?.sessionId || 'host returned no id'}.`;
  return 'Existing handover lookup is not wired yet. No session was opened.';
}

function applyLoadedSession(mode, payload, fallback) {
  applySessionPayload(payload);
  setActiveSessionContext({
    selectedShift: payload?.shiftCode || fallback.shiftCode,
    selectedShiftLabel: fallback.shiftLabel,
    activeSessionMode: mode,
    activeSessionDate: payload?.shiftDate || fallback.date,
    activeSessionStatus: payload?.sessionStatus || statusFromMode(mode),
    activeSessionId: payload?.sessionId ?? null,
    activeSessionSummary: {
      status: payload?.sessionStatus || statusFromMode(mode),
      updatedAt: payload?.updatedAt || payload?.createdAt || null,
      updatedBy: payload?.updatedBy || payload?.createdBy || fallback.userName
    }
  });
}

export function renderHandoverSessionScreen(root, state, sessionConfig) {
  const dateLabel = sessionConfig.date || new Date().toLocaleDateString();
  const sessionStatus = state.session?.sessionStatus || statusFromMode(sessionConfig.mode);
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
  const provider = readRuntime(runtime, ['effectiveProvider', 'EffectiveProvider'], readRuntime(runtime, ['mode'], 'Unknown'));
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
    createSummaryCard('Session ID', state.session?.sessionId || 'Not persisted yet'),
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
    Object.assign(createElement('button', 'btn btn-ghost', 'Save Draft (auto after edits)'), { disabled: true, title: 'Draft saving happens through Department/Budget/Attachment save flows.' }),
    createButton('Back to Shift Dashboard', 'btn btn-ghost', sessionConfig.dashboardRoute),
    createButton('Back to Home', 'btn btn-ghost', 'home')
  );
  section.append(actionsRow);

  const statusLine = createElement('p', 'status-line', state.session?.sessionId
    ? `Loaded active session context: ${state.session.sessionId}.`
    : 'No persisted session id yet. Use Start / Continue to load host session context.');
  section.append(statusLine);

  const workflowGrid = createElement('div', 'session-workflow-grid');
  workflowGrid.append(
    createActionCard('Department Board', 'departmentBoard', 'Supervisor handover board for this shift/session'),
    createActionCard('Budget', 'budgetMenu', 'Budget UI for active session context'),
    createActionCard('Attachments', 'attachments', 'Attachment metadata for active session'),
    createActionCard('Preview / Reports', 'reports', 'Preview saved handover package')
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
  root.querySelector('[data-action="start"]')?.addEventListener('click', async () => {
    if (sessionConfig.mode === 'open') {
      statusLine.className = 'status-line warn';
      statusLine.textContent = 'Open Existing handover picker is not wired yet. No session was opened.';
      return;
    }

    startButton.disabled = true;
    statusLine.className = 'status-line';
    statusLine.textContent = sessionConfig.mode === 'create' ? 'Creating session through host service…' : 'Loading existing draft through host service…';

    const userName = state.session?.updatedBy || state.session?.createdBy || 'Supervisor';
    const serviceDate = normaliseDateForService(dateLabel);

    try {
      const payload = sessionConfig.mode === 'create'
        ? await sessionService.createBlankSession(sessionConfig.shiftCode, serviceDate, userName)
        : await sessionService.openSession(sessionConfig.shiftCode, serviceDate, userName);
      applyLoadedSession(sessionConfig.mode, payload, {
        shiftCode: sessionConfig.shiftCode,
        shiftLabel: sessionConfig.shiftLabel,
        date: serviceDate,
        userName
      });
      statusLine.className = 'status-line success';
      statusLine.textContent = sessionStatusMessage(sessionConfig.mode, payload);
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'departmentBoard' } }));
    } catch (error) {
      startButton.disabled = false;
      statusLine.className = 'status-line warn';
      statusLine.textContent = error instanceof Error
        ? `Host persistence is not wired in this environment. No data was written. ${error.message}`
        : 'Host persistence is not wired in this environment. No data was written.';
    }
  });
}
