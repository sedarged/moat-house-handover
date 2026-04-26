import { diagnosticsService } from '../services/diagnosticsService.js';
import { auditService } from '../services/auditService.js';
import { getRuntimeStatus } from '../core/hostBridge.js';

function makeMeta(label, value) {
  const row = document.createElement('p');
  row.className = 'meta';
  row.textContent = `${label}: ${value || 'n/a'}`;
  return row;
}

function renderChecks(container, checks) {
  container.textContent = '';

  if (!Array.isArray(checks) || checks.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No diagnostic checks have run yet.';
    container.append(empty);
    return;
  }

  const list = document.createElement('ul');
  list.className = 'dept-list';

  checks.forEach((check) => {
    const item = document.createElement('li');
    const head = document.createElement('strong');
    head.textContent = `${check.checkName || 'check'} — ${(check.status || 'unknown').toUpperCase()}`;
    item.append(head);

    item.append(makeMeta('Message', check.message || ''));
    item.append(makeMeta('Details', check.details || ''));

    list.append(item);
  });

  container.append(list);
}

function renderAudit(container, entries) {
  container.textContent = '';

  if (!Array.isArray(entries) || entries.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No recent audit entries found.';
    container.append(empty);
    return;
  }

  const list = document.createElement('ul');
  list.className = 'dept-list';

  entries.forEach((entry) => {
    const item = document.createElement('li');

    const head = document.createElement('strong');
    head.textContent = `${entry.actionType || 'action'} @ ${entry.eventAt || 'n/a'}`;
    item.append(head);

    item.append(makeMeta('Entity', `${entry.entityType || ''} • ${entry.entityKey || ''}`));
    item.append(makeMeta('User', entry.userName || 'n/a'));
    item.append(makeMeta('Details', entry.details || ''));

    list.append(item);
  });

  container.append(list);
}

export function renderDiagnosticsScreen(root, state) {
  root.textContent = '';

  const panel = document.createElement('section');
  panel.className = 'panel';

  const title = document.createElement('h2');
  title.textContent = 'Runtime Diagnostics + Audit';
  panel.append(title);

  const hint = document.createElement('p');
  hint.className = 'meta';
  hint.textContent = 'Run Diagnostics first on each workstation before session testing. No email is sent here.';
  panel.append(hint);
  const boundary = document.createElement('p');
  boundary.className = 'meta';
  boundary.textContent = 'Outlook draft creation requires Windows + Outlook desktop. Access checks require local ACE/OLEDB runtime support.';
  panel.append(boundary);

  const msg = document.createElement('p');
  msg.className = 'meta';
  panel.append(msg);

  const actions = document.createElement('div');
  actions.className = 'actions-row';

  const runButton = document.createElement('button');
  runButton.type = 'button';
  runButton.textContent = 'Run Diagnostics';

  const auditButton = document.createElement('button');
  auditButton.type = 'button';
  auditButton.textContent = 'Refresh Audit';

  const logsButton = document.createElement('button');
  logsButton.type = 'button';
  logsButton.className = 'secondary';
  logsButton.textContent = 'Open Logs Folder';

  const backDashboard = document.createElement('button');
  backDashboard.type = 'button';
  backDashboard.className = 'secondary';
  backDashboard.textContent = 'Back to Dashboard';

  const backShift = document.createElement('button');
  backShift.type = 'button';
  backShift.className = 'secondary';
  backShift.textContent = 'Back to Shift';

  actions.append(runButton, auditButton, logsButton, backDashboard, backShift);
  panel.append(actions);

  const overall = document.createElement('div');
  panel.append(overall);

  const checksTitle = document.createElement('h3');
  checksTitle.textContent = 'Checks';
  panel.append(checksTitle);

  const checksWrap = document.createElement('div');
  panel.append(checksWrap);

  const auditTitle = document.createElement('h3');
  auditTitle.textContent = 'Recent Audit Log';
  panel.append(auditTitle);

  const auditWrap = document.createElement('div');
  panel.append(auditWrap);

  const pathsTitle = document.createElement('h3');
  pathsTitle.textContent = 'Runtime Paths';
  panel.append(pathsTitle);
  const pathsWrap = document.createElement('div');
  panel.append(pathsWrap);

  root.append(panel);

  const setBusy = (busy) => {
    runButton.disabled = busy;
    auditButton.disabled = busy;
    logsButton.disabled = busy;
  };

  const renderOverall = (payload) => {
    overall.textContent = '';
    overall.append(makeMeta('Overall status', (payload?.overallStatus || 'not-run').toUpperCase()));
    overall.append(makeMeta('Checked at', payload?.checkedAt || 'n/a'));

    const checks = Array.isArray(payload?.checks) ? payload.checks : [];
    const getStatus = (name) => checks.find((item) => item?.checkName === name)?.status || 'n/a';

    overall.append(makeMeta('Access database path', getStatus('access.database.path')));
    overall.append(makeMeta('Attachments root', getStatus('attachments.root.exists')));
    overall.append(makeMeta('Reports root', getStatus('reports.root.exists')));
    overall.append(makeMeta('Logs root', getStatus('logs.root.exists')));
    overall.append(makeMeta('Email profiles AM/PM/NS', getStatus('email.profiles.exist')));
    overall.append(makeMeta('Outlook availability', getStatus('outlook.com.available')));
  };

  const renderRuntimePaths = (status) => {
    pathsWrap.textContent = '';
    pathsWrap.append(makeMeta('Config file', status?.configPath || 'n/a'));
    pathsWrap.append(makeMeta('Access DB', status?.accessDatabasePath || 'n/a'));
    pathsWrap.append(makeMeta('Attachments root', status?.attachmentsRoot || 'n/a'));
    pathsWrap.append(makeMeta('Reports root', status?.reportsOutputRoot || 'n/a'));
    pathsWrap.append(makeMeta('Logs root', status?.logRoot || 'n/a'));
    pathsWrap.append(makeMeta('Asset root', status?.assetRoot || 'n/a'));
  };

  async function runDiagnostics() {
    setBusy(true);
    msg.textContent = 'Running diagnostics...';
    try {
      const payload = await diagnosticsService.run(state?.session?.userName || '');
      renderOverall(payload);
      renderChecks(checksWrap, payload?.checks || []);
      msg.textContent = `Diagnostics completed with status: ${(payload?.overallStatus || 'unknown').toUpperCase()}.`;
    } catch (error) {
      msg.textContent = error instanceof Error ? error.message : 'Diagnostics failed.';
      renderOverall(null);
      renderChecks(checksWrap, []);
    } finally {
      setBusy(false);
    }
  }

  async function loadAudit() {
    setBusy(true);
    msg.textContent = 'Loading recent audit entries...';
    try {
      const sessionId = Number(state?.session?.sessionId || 0);
      const result = sessionId > 0
        ? await auditService.listForSession(sessionId, 25)
        : await auditService.listRecent(25);
      renderAudit(auditWrap, result?.entries || []);
      msg.textContent = `Loaded ${Number(result?.entries?.length || 0)} audit entries.`;
    } catch (error) {
      msg.textContent = error instanceof Error ? error.message : 'Failed to load audit entries.';
      renderAudit(auditWrap, []);
    } finally {
      setBusy(false);
    }
  }

  runButton.addEventListener('click', runDiagnostics);
  auditButton.addEventListener('click', loadAudit);
  logsButton.addEventListener('click', async () => {
    setBusy(true);
    try {
      const result = await diagnosticsService.openLogsFolder();
      msg.textContent = `Opened logs folder: ${result?.openedPath || 'n/a'}`;
    } catch (error) {
      msg.textContent = error instanceof Error ? error.message : 'Failed to open logs folder.';
    } finally {
      setBusy(false);
    }
  });

  backDashboard.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  backShift.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'shift' } }));
  });

  renderOverall(null);
  getRuntimeStatus().then(renderRuntimePaths).catch(() => renderRuntimePaths(null));
  renderChecks(checksWrap, []);
  renderAudit(auditWrap, []);
  loadAudit();
}
