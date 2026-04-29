import { sessionService }   from '../services/sessionService.js';
import { budgetService }    from '../services/budgetService.js';
import {
  applyBudgetSummaryPayload,
  applySessionPayload,
  setActiveDepartmentName
} from '../state/appState.js';
import { metricDepartments } from '../models/contracts.js';
import {
  iconBrandSvg, iconBell, iconUser, iconArrowLeft, iconCalendar,
  iconClipboard, iconSpeedometer, iconTarget, iconClockSm, iconPaperclip,
  iconChart, iconCheckCircle, iconAlertCircle, iconPause, iconSave,
  iconEye, iconSend
} from '../core/icons.js';

/* ── helpers ── */
function formatDate(iso) {
  if (!iso) return '—';
  try {
    const [y, m, d] = iso.split('-');
    return `${d}/${m}/${y}`;
  } catch { return iso; }
}

function formatTime(isoOrStr) {
  if (!isoOrStr) return '—';
  const t = String(isoOrStr);
  const match = t.match(/(\d{2}:\d{2})/);
  return match ? match[1] : t.slice(0, 5);
}

function statusClass(s) {
  if (!s) return 'status-notupdated';
  const lc = s.toLowerCase().replace(/\s+/g, '');
  if (lc === 'complete') return 'status-complete';
  if (lc === 'incomplete') return 'status-incomplete';
  if (lc === 'notrunning') return 'status-notrunning';
  return 'status-notupdated';
}

function statusIcon(s) {
  if (!s) return iconClockSm;
  const lc = s.toLowerCase().replace(/\s+/g, '');
  if (lc === 'complete')   return iconCheckCircle;
  if (lc === 'incomplete') return iconAlertCircle;
  if (lc === 'notrunning') return iconPause;
  return iconClockSm;
}

function fmt(value, fallback = '—') {
  if (value == null || value === '') return fallback;
  const n = Number(value);
  return Number.isFinite(n) ? n.toFixed(1) : fallback;
}

/* ── Departments Completed (all 13 depts) ── */
function calcDepartmentsCompleted(departments) {
  let complete = 0, incomplete = 0, notRunning = 0;
  departments.forEach((d) => {
    const s = (d.deptStatus || 'Not running').toLowerCase().replace(/\s+/g, '');
    if (s === 'complete')    complete++;
    else if (s === 'incomplete') incomplete++;
    else notRunning++;
  });
  return { complete, incomplete, notRunning, total: departments.length };
}

/* ── Metric Summary (only Injection, MetaPress, Berks, Wilts) ── */
function calcMetricSummary(departments) {
  const metricDepts = departments.filter((d) => metricDepartments.includes(d.deptName));
  const totalDowntime = metricDepts.reduce((sum, d) => sum + (Number(d.downtimeMin) || 0), 0);
  const effValues   = metricDepts.map((d) => d.efficiencyPct).filter((v) => v != null && v !== '');
  const yieldValues = metricDepts.map((d) => d.yieldPct).filter((v) => v != null && v !== '');
  const avgEff   = effValues.length   ? effValues.reduce((a, b) => a + Number(b), 0) / effValues.length   : null;
  const avgYield = yieldValues.length ? yieldValues.reduce((a, b) => a + Number(b), 0) / yieldValues.length : null;
  return { totalDowntime, avgEff, avgYield };
}

/* ── main render ── */
export function renderDashboardScreen(root, state) {
  const { session } = state;

  if (!session?.sessionId) {
    root.innerHTML = `
      <div class="screen">
        <header class="screen-header">
          <div class="header-brand">${iconBrandSvg}<div class="header-brand-text">
            <span class="header-brand-sub">Moat House</span>
            <span class="header-brand-name">Operations</span>
          </div></div>
          <div class="header-title">MOAT HOUSE HANDOVER</div>
        </header>
        <div class="screen-content" style="display:flex;align-items:center;justify-content:center;">
          <div style="text-align:center;color:var(--muted)">
            <p style="font-size:1.1rem;margin-bottom:0.75rem">No active session</p>
            <p style="margin-bottom:1rem;font-size:0.88rem">Open a shift session to access the dashboard.</p>
            <button class="btn btn-primary" id="go-shift">Open Shift Screen</button>
          </div>
        </div>
      </div>`;
    root.querySelector('#go-shift')?.addEventListener('click', () => {
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'shift' } }));
    });
    return;
  }

  const departments  = session.departments || [];
  const deptComp     = calcDepartmentsCompleted(departments);
  const metricSumm   = calcMetricSummary(departments);
  const totalAttach  = departments.reduce((s, d) => s + Number(d.attachmentCount || 0), 0);
  const updatedCount = departments.filter((d) => d.updatedAt).length;

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  /* ── HEADER ── */
  const shiftLabel = session.shiftCode === 'NS' ? 'NIGHT' : session.shiftCode;
  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="hdr-back" title="Back to Shift" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">${shiftLabel} SHIFT HANDOVER</div>
      <div class="header-actions">
        <button class="header-icon-btn" id="hdr-bell" title="Notifications" type="button">
          ${iconBell}<span class="header-dot"></span>
        </button>
        <span class="header-divider"></span>
        <button class="header-icon-btn" id="hdr-user" title="User" type="button">${iconUser}</button>
      </div>
    </header>

    <div class="screen-infobar">
      <div class="infobar-item">
        <span class="infobar-icon">${iconCalendar}</span>
        <span>Date: ${formatDate(session.shiftDate)}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClockSm}</span>
        <span>Shift: ${session.shiftCode}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClipboard}</span>
        <span>Updated: ${updatedCount} / ${departments.length}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClockSm}</span>
        <span>Last updated: ${formatTime(session.updatedAt)}</span>
      </div>
    </div>

    <div class="screen-content" id="dashboard-content">

      <div class="section-block">
        <div class="section-header">
          <span class="section-icon">${iconClockSm}</span>
          <span class="section-title">Department Status</span>
        </div>
        <div class="dept-cards-grid" id="dept-cards-grid"></div>
      </div>

      <div class="shift-summary-section" id="shift-summary">
        <div class="section-header" style="margin-bottom:0;align-self:flex-start;white-space:nowrap;padding-right:1rem;min-width:fit-content;">
          <span class="section-icon">${iconChart}</span>
          <span class="section-title">Shift Summary</span>
        </div>
        <div class="summary-divider"></div>
        <div class="summary-metrics-group" id="summary-metrics"></div>
        <div class="summary-divider"></div>
        <div class="summary-side" id="summary-side"></div>
      </div>

      <p class="status-line" id="dashboard-message"></p>
    </div>

    <footer class="screen-footer">
      <button class="btn" id="footer-save"    type="button">${iconSave}&nbsp; Save</button>
      <button class="btn" id="footer-preview" type="button">${iconEye}&nbsp; Preview</button>
      <button class="btn btn-primary" id="footer-send" type="button">${iconSend}&nbsp; Save &amp; Send</button>
      <button class="btn" id="footer-budget"  type="button">${iconChart}&nbsp; Budget</button>
    </footer>
  `;

  root.append(screen);

  /* ── DEPT CARDS ── */
  const cardsGrid = screen.querySelector('#dept-cards-grid');
  buildDeptCards(cardsGrid, departments, session);

  /* ── SUMMARY METRICS ── */
  const metricsGroup = screen.querySelector('#summary-metrics');
  const summarySide  = screen.querySelector('#summary-side');
  buildSummaryMetrics(metricsGroup, summarySide, deptComp, metricSumm, totalAttach, state.budgetSummary);

  /* ── EVENT HANDLERS ── */
  screen.querySelector('#hdr-back')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'shift' } }));
  });
  screen.querySelector('#hdr-bell')?.addEventListener('click', () => {});
  screen.querySelector('#hdr-user')?.addEventListener('click', () => {});

  screen.querySelector('#footer-save')?.addEventListener('click', async () => {
    const msg = screen.querySelector('#dashboard-message');
    msg.textContent = 'Dashboard view — edit departments individually using the department cards.';
    msg.className   = 'status-line';
  });

  screen.querySelector('#footer-preview')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'preview' } }));
  });

  screen.querySelector('#footer-send')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'preview' } }));
  });

  screen.querySelector('#footer-budget')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'budget' } }));
  });

  /* ── load budget summary async ── */
  loadBudgetSummary(session.sessionId, summarySide, totalAttach, deptComp, metricSumm, state);
}

/* ── build dept card grid ── */
function buildDeptCards(container, departments, session) {
  container.innerHTML = '';

  if (!departments.length) {
    container.innerHTML = '<p class="status-line" style="grid-column:1/-1">No department rows loaded for this session.</p>';
    return;
  }

  departments.forEach((dept) => {
    const sClass = statusClass(dept.deptStatus);
    const sIcon  = statusIcon(dept.deptStatus);
    const label  = dept.deptStatus || 'Not updated';

    const card = document.createElement('div');
    card.className = 'dept-card';
    card.title     = `Open ${dept.deptName}`;
    card.innerHTML = `
      <div class="dept-card-name">${dept.deptName}</div>
      <div class="status-badge ${sClass}">${sIcon} ${label}</div>
    `;
    card.addEventListener('click', () => {
      setActiveDepartmentName(dept.deptName);
      window.dispatchEvent(new CustomEvent('app:navigate', {
        detail: { route: 'department', deptName: dept.deptName }
      }));
    });
    container.append(card);
  });
}

/* ── build summary metrics row ── */
function buildSummaryMetrics(metricsGroup, summarySide, deptComp, metricSumm, totalAttach, budgetSummary) {
  metricsGroup.innerHTML = `
    <div class="summary-metric">
      <span class="summary-metric-icon">${iconClipboard}</span>
      <div class="summary-metric-data">
        <div class="summary-metric-label">Departments Completed</div>
        <div class="summary-metric-value neutral">${deptComp.complete}</div>
      </div>
    </div>
    <div class="summary-metric">
      <span class="summary-metric-icon">${iconSpeedometer}</span>
      <div class="summary-metric-data">
        <div class="summary-metric-label">Total Efficiency</div>
        <div class="summary-metric-value">${metricSumm.avgEff != null ? metricSumm.avgEff.toFixed(1) + '%' : '—'}</div>
      </div>
    </div>
    <div class="summary-metric">
      <span class="summary-metric-icon">${iconTarget}</span>
      <div class="summary-metric-data">
        <div class="summary-metric-label">Total Yield</div>
        <div class="summary-metric-value">${metricSumm.avgYield != null ? metricSumm.avgYield.toFixed(1) + '%' : '—'}</div>
      </div>
    </div>
    <div class="summary-metric">
      <span class="summary-metric-icon">${iconClockSm}</span>
      <div class="summary-metric-data">
        <div class="summary-metric-label">Total Downtime</div>
        <div class="summary-metric-value neutral">${metricSumm.totalDowntime} min</div>
      </div>
    </div>
  `;

  renderSummarySide(summarySide, totalAttach, budgetSummary);
}

function renderSummarySide(container, totalAttach, budgetSummary) {
  const budgetStatus = budgetSummary?.status || 'Not set';
  const lcStatus = budgetStatus.toLowerCase();
  const budgetClass = lcStatus === 'over'
    ? 'value-orange'
    : (lcStatus === 'on target' || lcStatus === 'under')
      ? 'value-green'
      : 'badge-draft';

  container.textContent = '';

  const attachmentItem = document.createElement('div');
  attachmentItem.className = 'summary-side-item';
  attachmentItem.innerHTML = `${iconPaperclip} Attachments`;
  const attachmentStrong = document.createElement('strong');
  attachmentStrong.textContent = String(totalAttach ?? 0);
  attachmentItem.append(attachmentStrong);

  const budgetItem = document.createElement('div');
  budgetItem.className = 'summary-side-item';
  budgetItem.innerHTML = `${iconChart} Budget`;
  const budgetStrong = document.createElement('strong');
  budgetStrong.className = budgetClass;
  budgetStrong.textContent = budgetStatus;
  budgetItem.append(budgetStrong);

  const compact = document.createElement('div');
  compact.className = 'summary-side-item summary-side-budget-values';
  const required = document.createElement('span');
  required.textContent = `Req ${fmt(budgetSummary?.plannedTotal, '—')}`;
  const used = document.createElement('span');
  used.textContent = `Used ${fmt(budgetSummary?.usedTotal, '—')}`;
  const variance = document.createElement('span');
  const v = Number(budgetSummary?.varianceTotal ?? NaN);
  variance.textContent = Number.isFinite(v) ? `Var ${v > 0 ? '+' : ''}${v.toFixed(0)}` : 'Var —';
  compact.append(required, used, variance);

  const secondary = document.createElement('div');
  secondary.className = 'summary-side-item summary-side-budget-values';
  const linesPlanned = document.createElement('span');
  linesPlanned.textContent = `Lines ${fmt(budgetSummary?.linesPlanned, '—')}`;
  const register = document.createElement('span');
  register.textContent = `Register ${fmt(budgetSummary?.totalStaffOnRegister, '—')}`;
  const absent = document.createElement('span');
  absent.textContent = `Absent ${fmt(budgetSummary?.absentCount, '—')}`;
  const holiday = document.createElement('span');
  holiday.textContent = `Holiday ${fmt(budgetSummary?.holidayCount, '—')}`;
  const agency = document.createElement('span');
  agency.textContent = `Agency ${fmt(budgetSummary?.agencyUsedCount, '—')}`;
  secondary.append(linesPlanned, register, absent, holiday, agency);

  const updated = document.createElement('div');
  updated.className = 'summary-side-item summary-side-updated';
  if (budgetSummary?.lastUpdatedAt) {
    updated.textContent = `Updated ${budgetSummary.lastUpdatedAt}`;
  } else {
    updated.textContent = 'Updated —';
  }

  container.append(attachmentItem, budgetItem, compact, secondary, updated);
}

async function loadBudgetSummary(sessionId, summarySide, totalAttach, deptComp, metricSumm, state) {
  try {
    const summary = await budgetService.loadDashboardBudgetSummary(sessionId);
    applyBudgetSummaryPayload(summary);
    renderSummarySide(summarySide, totalAttach, summary);
  } catch {
    /* budget not yet saved — show "Not set" which is already the default */
  }
}
