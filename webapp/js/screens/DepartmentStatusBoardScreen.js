import { setActiveDepartmentName } from '../state/appState.js';

const HANDOVER_AREAS = [
  'Injection','MetaPress','Berks','Wilts','Racking','Butchery','Further Processing','Tumblers','Smoke Tumbler','Minimums & Samples','Goods In & Despatch','Dry Goods','Additional'
];

const FALLBACK_STATUS = {
  Injection: 'Completed', MetaPress: 'Incomplete', Berks: 'Incomplete', Wilts: 'Completed', Racking: 'Completed', Butchery: 'Not updated',
  'Further Processing': 'Incomplete', Tumblers: 'Not running', 'Smoke Tumbler': 'Not running', 'Minimums & Samples': 'Not updated',
  'Goods In & Despatch': 'Completed', 'Dry Goods': 'Incomplete', Additional: 'Not updated'
};

const statusClass = (status) => {
  const normal = String(status || '').toLowerCase();
  if (normal === 'completed' || normal === 'complete') return 'completed';
  if (normal === 'incomplete') return 'incomplete';
  if (normal === 'not running') return 'not-running';
  return 'not-updated';
};

const norm = (status) => (String(status || '').toLowerCase() === 'complete' ? 'Completed' : status || 'Not updated');

export function renderDepartmentStatusBoardScreen(root, state, boardConfig) {
  const date = boardConfig.sessionDate || state.activeSessionDate || new Date().toLocaleDateString();
  const shiftCode = boardConfig.shiftCode || state.selectedShift || 'AM';
  const shiftLabel = boardConfig.shiftLabel || (shiftCode === 'NS' ? 'Night Shift' : `${shiftCode} Shift`);
  const shiftTitle = shiftCode === 'NS' ? 'NIGHT SHIFT HANDOVER' : `${shiftCode} SHIFT HANDOVER`;
  const accent = boardConfig.accent || (shiftCode === 'NS' ? 'ns' : shiftCode.toLowerCase());

  const byName = new Map((state.session?.departments || []).map((d) => [d.deptName, d]));
  const boardItems = HANDOVER_AREAS.map((deptName) => {
    const found = byName.get(deptName);
    return { deptName, status: norm(found?.deptStatus || FALLBACK_STATUS[deptName]), updatedAt: found?.updatedAt || null, attachmentCount: found?.attachmentCount || 0, downtimeMin: found?.downtimeMin, efficiencyPct: found?.efficiencyPct, yieldPct: found?.yieldPct };
  });

  const completed = boardItems.filter((d) => statusClass(d.status) === 'completed').length;
  const updated = boardItems.filter((d) => ['completed', 'incomplete'].includes(statusClass(d.status))).length;
  const lastUpdated = boardItems.map((d) => d.updatedAt).filter(Boolean).sort().reverse()[0] || 'Not updated';

  const metrics = boardItems.filter((d) => typeof d.efficiencyPct === 'number' || typeof d.yieldPct === 'number');
  const avgEff = metrics.length ? `${(metrics.reduce((s, d) => s + (Number(d.efficiencyPct) || 0), 0) / metrics.length).toFixed(1)}%` : 'Not available (UI fallback)';
  const avgYield = metrics.length ? `${(metrics.reduce((s, d) => s + (Number(d.yieldPct) || 0), 0) / metrics.length).toFixed(1)}%` : 'Not available (UI fallback)';
  const downtime = metrics.length ? `${metrics.reduce((s, d) => s + (Number(d.downtimeMin) || 0), 0)} min` : '0 min (UI fallback)';
  const attachments = boardItems.reduce((s, d) => s + (Number(d.attachmentCount) || 0), 0);

  root.innerHTML = `<section class="department-board shift-${accent}">
    <div class="department-board-header">
      <div><p class="department-board-kicker">${shiftLabel}</p><h2>${shiftTitle}</h2><p>${boardConfig.sessionMode || state.activeSessionMode || 'Session'} · ${boardConfig.sessionStatus || state.activeSessionStatus || 'Draft in progress'}</p></div>
      <div class="department-board-nav"><button class="btn btn-ghost" data-nav="sessionContinue">Back to Handover Session</button><button class="btn btn-ghost" data-nav="${shiftCode === 'PM' ? 'pm' : shiftCode === 'NS' ? 'night' : 'am'}">Shift Dashboard</button><button class="btn btn-ghost" data-nav="home">Home</button></div>
    </div>
    <div class="department-board-info-strip">
      <span>Date: <strong>${date}</strong></span><span>Shift: <strong>${shiftCode}</strong></span><span>Updated: <strong>${updated} / ${boardItems.length}</strong></span><span>Last updated: <strong>${lastUpdated}</strong></span>
    </div>
    <section class="department-board-section"><h3>DEPARTMENT STATUS</h3><div class="department-board-grid">${boardItems.map((item) => `<article class="department-card"><h4>${item.deptName}</h4><span class="status-pill ${statusClass(item.status)}">${item.status}</span><button class="btn btn-secondary" data-dept="${item.deptName}" data-status="${item.status}">Open Department</button></article>`).join('')}</div></section>
    <section class="department-board-summary"><h3>SHIFT SUMMARY</h3><div class="department-board-summary-grid"><article><span>Departments Completed</span><strong>${completed} / ${boardItems.length}</strong></article><article><span>Total Efficiency</span><strong>${avgEff}</strong></article><article><span>Total Yield</span><strong>${avgYield}</strong></article><article><span>Total Downtime</span><strong>${downtime}</strong></article><article><span>Attachments count</span><strong>${attachments}</strong></article><article><span>Budget status</span><strong>Draft</strong></article></div></section>
    <div class="department-board-actions"><button class="btn btn-primary" disabled title="Save workflow comes next">Save</button><button class="btn btn-secondary" data-nav="reports">Preview</button><button class="btn btn-primary" disabled title="Send workflow remains legacy/future">Save & Send</button><button class="btn btn-secondary" data-nav="budgetMenu">Budget</button></div>
  </section>`;

  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }))));
  root.querySelectorAll('[data-dept]').forEach((button) => button.addEventListener('click', () => {
    setActiveDepartmentName(button.dataset.dept);
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'departmentDetailEntry' } }));
  }));
}
