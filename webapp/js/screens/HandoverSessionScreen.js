import { setActiveSessionContext } from '../state/appState.js';

function readRuntime(status, keys, fallback = 'Unknown') {
  for (const key of keys) {
    if (status && status[key] !== undefined && status[key] !== null && status[key] !== '') return status[key];
  }
  return fallback;
}

function summaryCard(label, value) {
  return `<article class="session-summary-card"><span>${label}</span><strong>${value}</strong></article>`;
}

function actionCard(label, route, note) {
  return `<button class="session-workflow-card" data-nav="${route}"><span>${label}</span><small>${note}</small></button>`;
}

function statusFromMode(mode) {
  if (mode === 'create') return 'Draft not started';
  if (mode === 'continue') return 'Draft in progress';
  return 'Existing handover lookup';
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

  root.innerHTML = `<section class="handover-session shift-${sessionConfig.accent}">
    <div class="session-header-card">
      <p class="session-kicker">${sessionConfig.shiftLabel}</p>
      <h2>${sessionConfig.shiftLabel} Handover Session</h2>
      <p>Hours: ${sessionConfig.hours} · Date: ${dateLabel}</p>
      <p>Mode: ${modeLabel} · Status: ${sessionStatus}</p>
    </div>

    <div class="session-summary-grid">
      ${summaryCard('Session date', dateLabel)}
      ${summaryCard('Shift', sessionConfig.shiftCode)}
      ${summaryCard('Supervisor/User', state.session?.updatedBy || state.session?.createdBy || 'Supervisor')}
      ${summaryCard('Status', sessionStatus)}
      ${summaryCard('Last saved', lastSaved)}
      ${summaryCard('Data provider', provider)}
    </div>

    <div class="session-actions-row">
      <button class="btn btn-primary" data-action="start">${modeLabel === 'Create' ? 'Start / Create Session' : modeLabel === 'Continue' ? 'Continue Session' : 'Open Session Lookup'}</button>
      <button class="btn btn-ghost" disabled>Save Draft (Phase 10F+)</button>
      <button class="btn btn-ghost" data-nav="${sessionConfig.dashboardRoute}">Back to Shift Dashboard</button>
      <button class="btn btn-ghost" data-nav="home">Back to Home</button>
    </div>

    <div class="session-workflow-grid">
      ${actionCard('Department Board', 'departmentBoard', 'Supervisor handover board for this shift/session')}
      ${actionCard('Budget', 'budgetMenu', 'Budget UI available via module menu')}
      ${actionCard('Attachments', 'attachments', 'Phase 10H full workflow')}
      ${actionCard('Preview / Reports', 'reports', 'Preview and reporting module')}
    </div>

    <div class="session-readiness-panel">
      <h3>Runtime / Readiness</h3>
      <div class="session-readiness-grid">
        <span>Active provider</span><strong>${provider}</strong>
        <span>Data root</span><strong title="${dataRoot}">${dataRoot}</strong>
        <span>Lock status</span><strong>${lockStatus}</strong>
        <span>Read / Write</span><strong>${canRead ? 'Read' : 'Blocked'} / ${canWrite ? 'Write' : 'Read-only'}</strong>
        <span>AccessLegacy</span><strong>${accessPath ? 'Ready' : 'Unavailable'}</strong>
        <span>SQLite readiness</span><strong>${sqliteReady ? 'Ready' : 'Not started'}</strong>
      </div>
    </div>
  </section>`;

  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
  }));

  root.querySelector('[data-action="start"]')?.addEventListener('click', () => {
    const nextRoute = sessionConfig.mode === 'open' ? 'history' : 'dashboard';
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: nextRoute } }));
  });
}
