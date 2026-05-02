
function readRuntime(status, keys, fallback = 'Unknown') {
  for (const key of keys) {
    if (status && status[key] !== undefined && status[key] !== null && status[key] !== '') return status[key];
  }
  return fallback;
}

function shiftCard(name, route, hours, cls) {
  return `<article class="mh-shift-card ${cls}"><div class="mh-shift-icon">◷</div><h3>${name}</h3><p>${hours}</p><span class="mh-ready">● Ready</span><button data-nav="${route}" class="btn btn-primary">Open</button></article>`;
}

function statusContent(state) {
  if (state.runtimeStatusError) return `<p class="status-line warn">Status unavailable: ${state.runtimeStatusError}</p>`;
  const s = state.runtimeStatus;
  if (!s) return `<p class="status-line warn">Status unavailable in dev mode.</p>`;
  const rootPath = readRuntime(s, ['approvedDataRoot', 'ApprovedDataRoot']);
  const provider = readRuntime(s, ['effectiveProvider', 'EffectiveProvider']);
  const sqliteReady = readRuntime(s, ['sqliteBootstrapSucceeded', 'SqliteBootstrapSucceeded'], false);
  const accessPath = readRuntime(s, ['accessDatabasePath', 'AccessDatabasePath'], '');
  const lockStatus = readRuntime(s, ['appLockStatus', 'AppLockStatus']);
  const lockMessage = readRuntime(s, ['appLockMessage', 'AppLockMessage'], '');
  const canRead = !!readRuntime(s, ['appCanRead', 'AppCanRead'], false);
  const canWrite = !!readRuntime(s, ['appCanWrite', 'AppCanWrite'], false);
  return `<div class="paths-grid"><span class="paths-label">Active data root</span><span class="paths-value" title="${rootPath}">${rootPath}</span>
  <span class="paths-label">Active provider</span><span class="paths-value">${provider}</span>
  <span class="paths-label">SQLite readiness</span><span class="paths-value">${sqliteReady ? 'Ready' : 'Warning'}</span>
  <span class="paths-label">AccessLegacy</span><span class="paths-value">${accessPath ? 'Available' : 'Unavailable'}</span>
  <span class="paths-label">Lock status</span><span class="paths-value">${lockStatus} — ${lockMessage}</span>
  <span class="paths-label">Read / Write</span><span class="paths-value">${canRead ? 'Read' : 'Blocked'} / ${canWrite ? 'Write' : 'Read-only'}</span></div>`;
}

export function renderHomeScreen(root, state) {
  root.innerHTML = `<section class="mh-home-wrap">
  <div class="mh-welcome-card"><h2>Welcome, Supervisor</h2><p>Select a shift to open the handover dashboard.</p></div>
  <div class="mh-shift-grid">${shiftCard('AM SHIFT','am','06:00 – 14:00','am')}${shiftCard('PM SHIFT','pm','14:00 – 22:00','pm')}${shiftCard('NIGHT SHIFT','night','22:00 – 06:00','ns')}</div>
  <div class="mh-quick-grid"><button data-nav="history" class="mh-quick">Continue Last Session</button><button data-nav="reports" class="mh-quick">Review Previous Handovers</button><button data-nav="budget" class="mh-quick">Budget Summary</button></div>
  <div class="section-block"><div class="section-header"><div class="section-title">System status</div></div>${statusContent(state)}</div>
  <div class="mh-utility-row"><button data-nav="settings" class="btn btn-ghost">Settings</button><button class="btn btn-ghost" disabled>Help</button><button class="btn btn-ghost" disabled>Exit</button></div>
</section>`;
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }))));
}
