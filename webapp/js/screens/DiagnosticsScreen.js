import { diagnosticsService } from '../services/diagnosticsService.js';
import { getRuntimeStatus } from '../core/hostBridge.js';
import { appState } from '../state/appState.js';

const STATUS_MAP = new Set(['Ready', 'Warning', 'Blocked', 'Not configured', 'Not available', 'Browser fallback', 'Future phase']);

function text(value, fallback = 'Not available') {
  if (value === undefined || value === null) return fallback;
  const s = String(value).trim();
  return s.length ? s : fallback;
}

function pick(obj, keys, fallback = null) {
  for (const key of keys) {
    if (obj && obj[key] !== undefined && obj[key] !== null && obj[key] !== '') return obj[key];
  }
  return fallback;
}

function labelStatus(value, fallback = 'Not available') {
  const t = text(value, fallback);
  return STATUS_MAP.has(t) ? t : fallback;
}

function createCard(title, rows) {
  const card = document.createElement('article');
  card.className = 'admin-card';
  const h3 = document.createElement('h3');
  h3.textContent = title;
  card.append(h3);
  const dl = document.createElement('dl');
  dl.className = 'admin-card-grid';
  rows.forEach(([k, v]) => {
    const dt = document.createElement('dt'); dt.textContent = k;
    const dd = document.createElement('dd'); dd.textContent = text(v, 'Not available');
    dl.append(dt, dd);
  });
  card.append(dl);
  return card;
}

export function renderDiagnosticsScreen(root, state) {
  const runtime = state?.runtimeStatus || {};
  root.replaceChildren();
  const section = document.createElement('section');
  section.className = 'admin-diagnostics-screen';

  const header = document.createElement('div');
  header.className = 'department-board-header';
  const title = document.createElement('div');
  title.innerHTML = '<p class="department-board-kicker">Admin only</p><h2>ADMIN / DIAGNOSTICS</h2>';
  const nav = document.createElement('div'); nav.className = 'department-board-nav';
  [['Home','home'],['Open Settings','settings']].forEach(([t,r])=>{ const b=document.createElement('button'); b.className='btn btn-ghost'; b.textContent=t; b.addEventListener('click',()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:r}}))); nav.append(b);});
  header.append(title, nav);

  const info = document.createElement('div');
  info.className = 'department-board-info-strip';
  [
    `Host bridge: ${text(pick(runtime,['mode','Mode'],'Browser fallback'))}`,
    `Effective provider: ${text(pick(runtime,['effectiveProvider','EffectiveProvider'],'Not available'))}`,
    `Data root: ${text(pick(runtime,['approvedDataRoot','ApprovedDataRoot'],'Not configured'))}`,
    `App lock: ${text(pick(runtime,['appLockStatus','AppLockStatus'],'Not available'))}`
  ].forEach((v)=>{ const span=document.createElement('span'); span.textContent=v; info.append(span); });

  const grid = document.createElement('div'); grid.className = 'admin-grid';
  grid.append(
    createCard('Runtime health', [
      ['Host bridge', labelStatus(runtime ? 'Ready' : 'Not available')],
      ['Runtime mode', text(pick(runtime,['mode','Mode'],'Not available'))],
      ['SQLite readiness', labelStatus(pick(runtime,['sqliteBootstrapSucceeded','SqliteBootstrapSucceeded']) ? 'Ready' : 'Warning','Warning')],
      ['AccessLegacy readiness', text(pick(runtime,['accessDatabasePath','AccessDatabasePath']) ? 'Ready' : 'Not available')]
    ]),
    createCard('Provider / database', [
      ['Effective provider', pick(runtime,['effectiveProvider','EffectiveProvider'],'Not available')],
      ['Configured provider', pick(runtime,['configuredProvider','ConfiguredProvider'],'Not available')],
      ['Provider switching', 'Provider switching is not performed from this screen.']
    ]),
    createCard('Data root / storage', [
      ['Approved data root', pick(runtime,['approvedDataRoot','ApprovedDataRoot'],'Not configured')],
      ['Reports folder', pick(runtime,['reportsRoot','ReportsRoot'],'Folder detail not available from host diagnostics.')],
      ['Attachments folder', pick(runtime,['attachmentsRoot','AttachmentsRoot'],'Folder detail not available from host diagnostics.')],
      ['Backups folder', pick(runtime,['backupsRoot','BackupsRoot'],'Folder detail not available from host diagnostics.')]
    ]),
    createCard('Lock / concurrency', [
      ['App lock status', pick(runtime,['appLockStatus','AppLockStatus'],'Not available')],
      ['Lock owner', pick(runtime,['appLockOwner','AppLockOwner'],'Not available')],
      ['Lock age', pick(runtime,['appLockAge','AppLockAge'],'Not available')],
      ['Write safety', pick(runtime,['appCanWrite','AppCanWrite']) ? 'Ready' : 'Warning']
    ]),
    createCard('Reports / attachments readiness', [
      ['Reports folder', pick(runtime,['reportsRootReady','ReportsRootReady']) ? 'Ready' : 'Warning'],
      ['Attachments folder', pick(runtime,['attachmentsRootReady','AttachmentsRootReady']) ? 'Ready' : 'Warning']
    ]),
    createCard('Backup / restore readiness', [
      ['Backup readiness', labelStatus(pick(runtime,['backupReadiness','BackupReadiness'],'Future phase'),'Future phase')],
      ['Restore readiness', labelStatus(pick(runtime,['restoreReadiness','RestoreReadiness'],'Future phase'),'Future phase')],
      ['Rollback readiness', labelStatus(pick(runtime,['rollbackReadiness','RollbackReadiness'],'Future phase'),'Future phase')]
    ]),
    createCard('Email / send readiness', [
      ['Email profile', labelStatus(pick(runtime,['emailProfileReady','EmailProfileReady']) ? 'Ready' : 'Not configured','Not configured')],
      ['Send service', labelStatus(pick(runtime,['sendServiceReady','SendServiceReady']) ? 'Ready' : 'Not available')],
      ['Outlook / COM', text(pick(runtime,['outlookStatus','OutlookStatus'],'Not available'))]
    ])
  );

  const panel = document.createElement('div'); panel.className = 'admin-output';
  const panelTitle = document.createElement('h3'); panelTitle.textContent = 'Diagnostics output / status';
  const output = document.createElement('p'); output.className = 'status-line'; output.textContent = text(appState.activeDiagnosticsStatus,'No diagnostics action run yet.');
  panel.append(panelTitle, output);

  const actions = document.createElement('div'); actions.className = 'department-board-actions';
  const actionDefs = [
    ['Refresh Diagnostics', async ()=>{ await diagnosticsService.run(state?.session?.userName||''); output.textContent='Refresh Diagnostics: Ready'; }],
    ['Check Runtime Status', async ()=>{ await getRuntimeStatus(); output.textContent='Check Runtime Status: Ready'; }],
    ['Check Data Root', async ()=>{ output.textContent='Check Data Root: Host diagnostic action is not wired in this environment.'; }],
    ['Check Report Folders', async ()=>{ output.textContent='Check Report Folders: Host diagnostic action is not wired in this environment.'; }],
    ['Check Backup Readiness', async ()=>{ output.textContent='Check Backup Readiness: Host diagnostic action is not wired in this environment.'; }],
    ['Open Settings', async ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'settings'}}))],
    ['Home', async ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'home'}}))]
  ];
  actionDefs.forEach(([label, fn])=>{ const b=document.createElement('button'); b.className='btn btn-primary'; b.textContent=label; b.addEventListener('click', async ()=>{ try{ await fn(); }catch{ output.textContent=`${label}: Host diagnostic action is not wired in this environment.`; } }); actions.append(b); });

  section.append(header, info, grid, panel, actions);
  root.append(section);
}
