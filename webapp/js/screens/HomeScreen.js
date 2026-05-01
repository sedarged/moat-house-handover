import { routes } from '../core/router.js';

function pill(value, cls = 'ready') {
  return `<span class="status-pill ${cls}" title="${value || ''}">${value || 'Unknown'}</span>`;
}

function runtimeMarkup(state) {
  if (state.runtimeStatusError) return `<div class="status-warning">Status unavailable: ${state.runtimeStatusError}</div>`;
  const s = state.runtimeStatus;
  if (!s) return '<div class="status-warning">Status unavailable in dev mode.</div>';
  const rw = s.appCanWrite ? 'Ready' : (s.appCanRead ? 'Read-only' : 'Blocked');
  return `<dl class="status-grid">
    <dt>Provider</dt><dd>${s.effectiveProvider} (requested: ${s.requestedProvider})</dd>
    <dt>Data root</dt><dd title="${s.approvedDataRoot}">${s.approvedDataRoot}</dd>
    <dt>SQLite</dt><dd title="${s.targetSqlitePath}">${s.sqliteBootstrapSucceeded ? 'Ready' : 'Warning'}</dd>
    <dt>AccessLegacy</dt><dd>${s.accessDatabasePath ? 'Available' : 'Unavailable'}</dd>
    <dt>Lock</dt><dd>${pill(s.appLockStatus, s.appCanWrite ? 'unlocked' : 'locked')} ${s.appLockMessage || ''}</dd>
    <dt>Read/Write</dt><dd>${pill(rw, s.appCanWrite ? 'ready' : (s.appCanRead ? 'readonly' : 'blocked'))}</dd>
  </dl>`;
}

export function renderHomeScreen(root, state) {
  root.innerHTML = `<div class="home-screen">
    <section class="welcome-card"><h1>Moat House Handover</h1><p>Create, review and export shift handovers.</p></section>
    <section class="shift-grid">
      ${['AM','PM','NS'].map((s)=>`<article class="shift-card ${s.toLowerCase()}"><h2>${s === 'NS' ? 'Night Shift Handover' : `${s} Handover`}</h2><div class="shift-actions"><button data-nav="${s==='AM'?'am':s==='PM'?'pm':'night'}">Create / Open</button><button data-nav="${s==='AM'?'am':s==='PM'?'pm':'night'}">Continue Draft</button><button data-nav="${s==='AM'?'am':s==='PM'?'pm':'night'}">Preview</button></div></article>`).join('')}
    </section>
    <section class="quick-grid">
      ${['reports','budget','attachments','history','admin'].map((k)=>`<button data-nav="${k}">${routes[k].label}</button>`).join('')}
    </section>
    <section class="system-card"><h3>System status</h3>${runtimeMarkup(state)}</section>
  </div>`;

  root.querySelectorAll('[data-nav]').forEach((button) => {
    button.addEventListener('click', () => {
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
    });
  });
}
