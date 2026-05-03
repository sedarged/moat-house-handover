import { appState } from '../state/appState.js';

function read(obj, keys, fallback = 'Not configured') {
  for (const k of keys) {
    if (obj && obj[k] !== undefined && obj[k] !== null && obj[k] !== '') return String(obj[k]);
  }
  return fallback;
}

export function renderSettingsScreen(root, state) {
  const runtime = state?.runtimeStatus || {};
  root.replaceChildren();
  const section = document.createElement('section');
  section.className = 'settings-screen';
  section.innerHTML = `<div class="department-board-header"><div><p class="department-board-kicker">Admin only</p><h2>SETTINGS</h2><p>Critical settings are controlled by host/config and are not edited here yet.</p></div></div>`;

  const grid = document.createElement('div');
  grid.className = 'admin-grid';
  const rows = [
    ['App data root', read(runtime,['approvedDataRoot','ApprovedDataRoot'])],
    ['Provider mode', read(runtime,['effectiveProvider','EffectiveProvider'],'Not available')],
    ['Reports path', read(runtime,['reportsRoot','ReportsRoot'])],
    ['Attachments path', read(runtime,['attachmentsRoot','AttachmentsRoot'])],
    ['Email profile readiness', read(runtime,['emailProfileStatus','EmailProfileStatus'],'Not configured')],
    ['App version/build', read(runtime,['appVersion','AppVersion'],'Not available')]
  ];
  const card = document.createElement('article'); card.className='admin-card';
  const h = document.createElement('h3'); h.textContent='Configuration / readiness';
  const dl = document.createElement('dl'); dl.className='admin-card-grid';
  rows.forEach(([k,v])=>{ const dt=document.createElement('dt'); dt.textContent=k; const dd=document.createElement('dd'); dd.textContent=v; dl.append(dt,dd); });
  card.append(h,dl); grid.append(card);

  const status = document.createElement('p'); status.className='status-line'; status.textContent = appState.activeSettingsStatus || 'Settings loaded.';
  const actions = document.createElement('div'); actions.className='department-board-actions';
  [['Refresh Settings', ()=>{ appState.activeSettingsStatus = 'Refresh Settings: Host diagnostic action is not wired in this environment.'; status.textContent = appState.activeSettingsStatus; }],['Back to Admin / Diagnostics', ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'admin'}}))],['Home', ()=>window.dispatchEvent(new CustomEvent('app:navigate',{detail:{route:'home'}}))]].forEach(([t,fn])=>{ const b=document.createElement('button'); b.className='btn btn-primary'; b.textContent=t; b.addEventListener('click',fn); actions.append(b); });

  section.append(grid, status, actions);
  root.append(section);
}
