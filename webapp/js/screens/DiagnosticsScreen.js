import { diagnosticsService } from '../services/diagnosticsService.js';
import { auditService        } from '../services/auditService.js';
import { getRuntimeStatus    } from '../core/hostBridge.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar,
  iconClockSm, iconRefresh, iconExternalLink, iconFolder
} from '../core/icons.js';

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function checkBadgeClass(status) {
  const s = (status || '').toLowerCase();
  if (s === 'ok')      return 'check-ok';
  if (s === 'warning') return 'check-warning';
  if (s === 'failed')  return 'check-failed';
  return 'check-unknown';
}

function overallClass(status) {
  const s = (status || '').toLowerCase();
  if (s === 'ok')      return 'value-green';
  if (s === 'warning') return 'value-orange';
  if (s === 'failed')  return 'value-red';
  return 'value-muted';
}

export function renderDiagnosticsScreen(root, state) {
  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="diag-back-hdr" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">RUNTIME DIAGNOSTICS</div>
      <div class="header-actions">
        <button class="header-icon-btn" type="button">${iconBell}</button>
        <span class="header-divider"></span>
        <button class="header-icon-btn" type="button">${iconUser}</button>
      </div>
    </header>

    <div class="screen-infobar">
      <div class="infobar-item">
        <span class="infobar-icon">${iconCalendar}</span>
        <span>Date: ${formatDate(state?.session?.shiftDate) || 'No session'}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClockSm}</span>
        <span id="diag-overall-badge">Overall: —</span>
      </div>
      <div class="infobar-item">
        <span style="font-size:0.78rem;color:var(--muted);">Run diagnostics before first session on each workstation</span>
      </div>
    </div>

    <div class="screen-content">
      <p class="status-line" id="diag-message" style="margin-bottom:0.75rem;">
        Run Diagnostics to check Windows · WebView2 · Access · Outlook environment.
      </p>

      <div class="two-col">
        <!-- LEFT: checks + paths -->
        <div>
          <div class="section-block">
            <div class="section-header">
              <span class="section-title">Diagnostic Checks</span>
              <span id="checked-at" style="margin-left:auto;font-size:0.75rem;color:var(--muted);"></span>
            </div>
            <ul class="checks-list" id="checks-list">
              <li style="color:var(--muted);font-size:0.85rem;padding:0.5rem 0;">Press Run Diagnostics to start.</li>
            </ul>
          </div>

          <div class="section-block">
            <div class="section-header">
              <span class="section-title">Runtime Paths</span>
            </div>
            <div class="paths-grid" id="paths-grid">
              <span class="paths-label">Config file</span><span class="paths-value" id="path-config">—</span>
              <span class="paths-label">Access DB</span><span class="paths-value"   id="path-db">—</span>
              <span class="paths-label">Attachments</span><span class="paths-value" id="path-attach">—</span>
              <span class="paths-label">Reports</span><span class="paths-value"     id="path-reports">—</span>
              <span class="paths-label">Logs</span><span class="paths-value"        id="path-logs">—</span>
              <span class="paths-label">Assets</span><span class="paths-value"      id="path-assets">—</span>
            </div>
          </div>
        </div>

        <!-- RIGHT: audit -->
        <div>
          <div class="section-block" style="height:100%;">
            <div class="section-header">
              <span class="section-title">Recent Audit Log</span>
            </div>
            <ul class="audit-list" id="audit-list">
              <li style="color:var(--muted);font-size:0.85rem;padding:0.5rem 0;">Loading audit entries…</li>
            </ul>
          </div>
        </div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost" id="diag-back-shift"     type="button">${iconArrowLeft}&nbsp; Shift</button>
      <button class="btn btn-ghost" id="diag-back-dashboard" type="button">${iconArrowLeft}&nbsp; Dashboard</button>
      <button class="btn btn-primary" id="diag-run"          type="button">${iconRefresh}&nbsp; Run Diagnostics</button>
      <button class="btn" id="diag-refresh-audit"            type="button">${iconRefresh}&nbsp; Refresh Audit</button>
      <button class="btn btn-ghost" id="diag-open-logs"      type="button">${iconExternalLink}&nbsp; Open Logs Folder</button>
    </footer>
  `;

  root.append(screen);

  const msg          = screen.querySelector('#diag-message');
  const checksList   = screen.querySelector('#checks-list');
  const auditList    = screen.querySelector('#audit-list');
  const overallBadge = screen.querySelector('#diag-overall-badge');
  const checkedAt    = screen.querySelector('#checked-at');

  const nav = (route) => () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
  screen.querySelector('#diag-back-hdr')?.addEventListener('click', nav('shift'));
  screen.querySelector('#diag-back-shift')?.addEventListener('click', nav('shift'));
  screen.querySelector('#diag-back-dashboard')?.addEventListener('click', nav('dashboard'));

  const setBusy = (busy) => {
    screen.querySelector('#diag-run').disabled            = busy;
    screen.querySelector('#diag-refresh-audit').disabled  = busy;
    screen.querySelector('#diag-open-logs').disabled      = busy;
  };

  /* ── render checks ── */
  function renderChecks(checks) {
    checksList.innerHTML = '';
    if (!Array.isArray(checks) || !checks.length) {
      checksList.innerHTML = '<li style="color:var(--muted);font-size:0.85rem;padding:0.5rem 0;">No checks run yet.</li>';
      return;
    }
    checks.forEach((c) => {
      const li = document.createElement('li');
      li.className = 'check-row';
      li.innerHTML = `
        <span class="check-badge ${checkBadgeClass(c.status)}">${c.status || '?'}</span>
        <div class="check-content">
          <div class="check-name">${c.checkName || 'check'}</div>
          ${c.message ? `<div class="check-msg">${c.message}</div>` : ''}
          ${c.details  ? `<div class="check-msg" style="font-size:0.73rem;">${c.details}</div>` : ''}
        </div>
      `;
      checksList.append(li);
    });
  }

  /* ── render audit ── */
  function renderAudit(entries) {
    auditList.innerHTML = '';
    if (!Array.isArray(entries) || !entries.length) {
      auditList.innerHTML = '<li style="color:var(--muted);font-size:0.85rem;padding:0.5rem 0;">No recent audit entries.</li>';
      return;
    }
    entries.slice(0, 30).forEach((entry) => {
      const li = document.createElement('li');
      li.className = 'audit-row';
      li.innerHTML = `
        <div class="audit-action">${entry.actionType || 'action'}</div>
        <div class="audit-meta">
          ${entry.eventAt || '—'} · ${entry.entityType || ''} ${entry.entityKey ? `#${entry.entityKey}` : ''} · ${entry.userName || 'n/a'}
          ${entry.details ? `<br><span style="font-size:0.73rem;">${entry.details}</span>` : ''}
        </div>
      `;
      auditList.append(li);
    });
  }

  /* ── render runtime paths ── */
  function renderPaths(status) {
    screen.querySelector('#path-config').textContent  = status?.configPath          || '—';
    screen.querySelector('#path-db').textContent      = status?.accessDatabasePath  || '—';
    screen.querySelector('#path-attach').textContent  = status?.attachmentsRoot     || '—';
    screen.querySelector('#path-reports').textContent = status?.reportsOutputRoot   || '—';
    screen.querySelector('#path-logs').textContent    = status?.logRoot             || '—';
    screen.querySelector('#path-assets').textContent  = status?.assetRoot           || '—';
  }

  /* ── run diagnostics ── */
  screen.querySelector('#diag-run')?.addEventListener('click', async () => {
    setBusy(true);
    msg.textContent = 'Running diagnostics…';
    msg.className   = 'status-line';
    try {
      const payload = await diagnosticsService.run(state?.session?.userName || '');
      renderChecks(payload?.checks || []);
      const overall = (payload?.overallStatus || 'unknown').toUpperCase();
      overallBadge.innerHTML = `Overall: <strong class="${overallClass(payload?.overallStatus)}">${overall}</strong>`;
      checkedAt.textContent  = payload?.checkedAt ? `Checked: ${payload.checkedAt}` : '';
      msg.textContent = `Diagnostics complete — ${overall}.`;
      msg.className   = overall === 'OK' ? 'status-line success' : overall === 'WARNING' ? 'status-line warn' : 'status-line error';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Diagnostics failed.';
      msg.className   = 'status-line error';
      renderChecks([]);
    } finally { setBusy(false); }
  });

  /* ── refresh audit ── */
  async function loadAudit() {
    setBusy(true);
    msg.textContent = 'Loading audit entries…';
    msg.className   = 'status-line';
    try {
      const sessionId = Number(state?.session?.sessionId || 0);
      const result = sessionId > 0
        ? await auditService.listForSession(sessionId, 25)
        : await auditService.listRecent(25);
      renderAudit(result?.entries || []);
      msg.textContent = `Loaded ${result?.entries?.length || 0} audit entries.`;
      msg.className   = 'status-line success';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to load audit.';
      msg.className   = 'status-line error';
      renderAudit([]);
    } finally { setBusy(false); }
  }

  screen.querySelector('#diag-refresh-audit')?.addEventListener('click', loadAudit);

  screen.querySelector('#diag-open-logs')?.addEventListener('click', async () => {
    setBusy(true);
    msg.textContent = 'Opening logs folder…';
    try {
      const result = await diagnosticsService.openLogsFolder();
      msg.textContent = `Opened: ${result?.openedPath || 'n/a'}`;
      msg.className   = 'status-line success';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to open logs folder.';
      msg.className   = 'status-line error';
    } finally { setBusy(false); }
  });

  /* initial load */
  getRuntimeStatus().then(renderPaths).catch(() => renderPaths(null));
  loadAudit();
}
