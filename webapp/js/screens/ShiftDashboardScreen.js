import { setSelectedShift, setActiveSessionContext } from '../state/appState.js';

function readRuntime(status, keys, fallback = 'Unknown') {
  for (const key of keys) {
    if (status && status[key] !== undefined && status[key] !== null && status[key] !== '') return status[key];
  }
  return fallback;
}

function badge(label, tone = 'default') {
  return `<span class="shift-dashboard-badge ${tone}">${label}</span>`;
}

function actionCard(label, route, detail = '') {
  return `<button class="shift-action-card" data-nav="${route}"><span class="shift-action-title">${label}</span><span class="shift-action-detail">${detail}</span></button>`;
}

export function renderShiftDashboardScreen(root, state, shiftConfig) {
  setSelectedShift(shiftConfig.shiftCode);

  const dateLabel = new Date().toLocaleDateString();
  const runtime = state.runtimeStatus;

  const provider = readRuntime(runtime, ['effectiveProvider', 'EffectiveProvider'], 'Unknown');
  const dataRoot = readRuntime(runtime, ['approvedDataRoot', 'ApprovedDataRoot'], 'Unknown');
  const lockStatus = readRuntime(runtime, ['appLockStatus', 'AppLockStatus'], 'Status unavailable');
  const canRead = !!readRuntime(runtime, ['appCanRead', 'AppCanRead'], false);
  const canWrite = !!readRuntime(runtime, ['appCanWrite', 'AppCanWrite'], false);
  const sqliteReady = !!readRuntime(runtime, ['sqliteBootstrapSucceeded', 'SqliteBootstrapSucceeded'], false);
  const accessPath = readRuntime(runtime, ['accessDatabasePath', 'AccessDatabasePath'], '');

  root.innerHTML = `<section class="shift-dashboard shift-${shiftConfig.accent}">
    <div class="shift-dashboard-header-card">
      <p class="shift-dashboard-kicker">${shiftConfig.label}</p>
      <h2>${shiftConfig.title}</h2>
      <p class="shift-dashboard-hours">Hours: ${shiftConfig.hours}</p>
      <p class="shift-dashboard-meta">${dateLabel} · Status: ${shiftConfig.statusLabel}</p>
      <p class="shift-dashboard-desc">${shiftConfig.description}</p>
    </div>

    <div class="shift-dashboard-grid primary-grid">
      ${actionCard("Create Today's Handover", 'sessionCreate', 'Open shift/date session context')}
      ${actionCard('Continue Draft', 'sessionContinue', 'Continue in-progress handover for this shift')}
      ${actionCard('Open Existing Handover', 'sessionOpen', 'Open existing handover lookup session')}
    </div>

    <div class="shift-dashboard-grid workflow-grid">
      ${actionCard('Department Board', 'dashboard', 'Legacy dashboard entry · Phase 10F next')}
      ${actionCard('Budget', 'budgetMenu', 'Budget module menu')}
      ${actionCard('Attachments', 'attachments', 'Attachments module')}
      ${actionCard('Preview / Reports', 'reports', 'Preview and report routes')}
    </div>

    <div class="shift-dashboard-grid secondary-grid">
      ${actionCard('History / Previous Handovers', 'history', 'Search existing handovers')}
      <article class="shift-note-card"><h3>Shift Notes</h3><p>Use this dashboard to start or continue handover tasks. Full session editing is delivered in Phase 10E.</p></article>
      ${actionCard('Back to Home', 'home', 'Return to shift menu')}
    </div>

    <div class="shift-status-panel">
      <h3>Shift Readiness</h3>
      <div class="shift-status-grid">
        <span>Active provider</span><strong>${provider}</strong>
        <span>Data root</span><strong title="${dataRoot}">${dataRoot}</strong>
        <span>Lock status</span><strong>${lockStatus}</strong>
        <span>Read / Write</span><strong>${canRead ? 'Read' : 'Blocked'} / ${canWrite ? 'Write' : 'Read-only'}</strong>
        <span>AccessLegacy</span><strong>${accessPath ? 'Ready' : 'Unavailable'}</strong>
        <span>SQLite readiness</span><strong>${sqliteReady ? 'Ready' : 'Not started'}</strong>
      </div>
      <div class="shift-status-badges">${badge(shiftConfig.shiftCode, shiftConfig.accent)}${badge(shiftConfig.statusLabel)}${badge(provider.includes('SQLite') ? 'SQLite active' : 'AccessLegacy active')}</div>
    </div>
  </section>`;

  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    const route = button.dataset.nav;
    if (route === 'sessionCreate' || route === 'sessionContinue' || route === 'sessionOpen') {
      const mode = route === 'sessionCreate' ? 'create' : route === 'sessionContinue' ? 'continue' : 'open';
      setActiveSessionContext({
        selectedShift: shiftConfig.shiftCode,
        selectedShiftLabel: shiftConfig.label,
        activeSessionMode: mode,
        activeSessionDate: dateLabel,
        activeSessionStatus: mode === 'create' ? 'Draft not started' : mode === 'continue' ? 'Draft in progress' : 'Existing handover lookup'
      });
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route, shiftCode: shiftConfig.shiftCode, shiftLabel: shiftConfig.label, accent: shiftConfig.accent } }));
      return;
    }
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
  }));
}
