function shiftCard(name, route, hours, cls) {
  return `<article class="mh-shift-card ${cls}"><div class="mh-shift-icon">◷</div><h3>${name}</h3><p>${hours}</p><span class="mh-ready">● Ready</span><button data-nav="${route}" class="btn btn-primary">Open</button></article>`;
}

function statusContent(state) {
  if (state.runtimeStatusError) return `<p class="status-line warn">Status unavailable: ${state.runtimeStatusError}</p>`;
  const s = state.runtimeStatus;
  if (!s) return `<p class="status-line warn">Status unavailable in dev mode.</p>`;
  return `<div class="paths-grid"><span class="paths-label">Active data root</span><span class="paths-value" title="${s.approvedDataRoot}">${s.approvedDataRoot}</span>
  <span class="paths-label">Active provider</span><span class="paths-value">${s.effectiveProvider}</span>
  <span class="paths-label">SQLite readiness</span><span class="paths-value">${s.sqliteBootstrapSucceeded ? 'Ready' : 'Warning'}</span>
  <span class="paths-label">AccessLegacy</span><span class="paths-value">${s.accessDatabasePath ? 'Available' : 'Unavailable'}</span>
  <span class="paths-label">Lock status</span><span class="paths-value">${s.appLockStatus} — ${s.appLockMessage || ''}</span>
  <span class="paths-label">Read / Write</span><span class="paths-value">${s.appCanRead ? 'Read' : 'Blocked'} / ${s.appCanWrite ? 'Write' : 'Read-only'}</span></div>`;
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
