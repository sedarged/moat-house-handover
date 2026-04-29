import { previewService  } from '../services/previewService.js';
import { reportsService  } from '../services/reportsService.js';
import { applyPreviewPayload, appendGeneratedReport } from '../state/appState.js';
import { metricDepartments } from '../models/contracts.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar, iconClockSm,
  iconClipboard, iconChart, iconPaperclip, iconSave, iconSend, iconExternalLink,
  iconRefresh, iconFolder, iconSpeedometer, iconTarget, iconCheckCircle, iconAlertCircle, iconPause
} from '../core/icons.js';

function fmt(value, suffix = '') {
  if (value == null || value === '') return '—';
  const n = Number(value);
  return Number.isFinite(n) ? n.toFixed(1) + suffix : '—';
}

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function statusIcon(s) {
  const lc = (s || '').toLowerCase().replace(/\s+/g, '');
  if (lc === 'complete')   return iconCheckCircle;
  if (lc === 'incomplete') return iconAlertCircle;
  if (lc === 'notrunning') return iconPause;
  return iconClockSm;
}

function statusCls(s) {
  const lc = (s || '').toLowerCase().replace(/\s+/g, '');
  if (lc === 'complete')   return 'value-green';
  if (lc === 'incomplete') return 'value-orange';
  return 'value-muted';
}

function budgetStatusCls(s) {
  const lc = (s || '').toLowerCase();
  if (lc === 'over')      return 'value-orange';
  if (lc === 'on target' || lc === 'under') return 'value-green';
  return 'value-muted';
}

function fieldRow(label, value, valueCls = '') {
  return `<div class="field-row">
    <span class="field-label">${label}</span>
    <span class="field-value ${valueCls}">${value || '—'}</span>
  </div>`;
}

export function renderPreviewScreen(root, state) {
  const sessionId = state?.session?.sessionId;

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="prev-back-hdr" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">HANDOVER PREVIEW</div>
      <div class="header-actions">
        <button class="header-icon-btn" type="button">${iconBell}</button>
        <span class="header-divider"></span>
        <button class="header-icon-btn" type="button">${iconUser}</button>
      </div>
    </header>

    <div class="screen-infobar">
      <div class="infobar-item">
        <span class="infobar-icon">${iconCalendar}</span>
        <span>Date: ${formatDate(state?.session?.shiftDate)}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClockSm}</span>
        <span>Shift: ${state?.session?.shiftCode || '—'}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconClipboard}</span>
        <span>Read-only — saved state only</span>
      </div>
    </div>

    <div class="screen-content">
      <p class="status-line" id="prev-message" style="margin-bottom:0.75rem;"></p>

      ${!sessionId ? `<div class="section-block"><p class="status-line error">No active session loaded. Open a shift session first.</p></div>` : ''}

      <!-- Session header -->
      <div class="section-block" id="section-session" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconClipboard}</span>
          <span class="section-title">Session Header</span>
        </div>
        <div id="session-fields"></div>
      </div>

      <!-- Departments Completed -->
      <div class="section-block" id="section-depts-completed" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconCheckCircle}</span>
          <span class="section-title">Departments Completed</span>
        </div>
        <div id="depts-completed-fields"></div>
      </div>

      <!-- Department summaries -->
      <div class="section-block" id="section-depts" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconClipboard}</span>
          <span class="section-title">Department Summaries</span>
        </div>
        <div id="dept-rows"></div>
      </div>

      <!-- Metric Summary (metric depts only) -->
      <div class="section-block" id="section-metrics" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconSpeedometer}</span>
          <span class="section-title">Metric Summary (Injection · MetaPress · Berks · Wilts)</span>
        </div>
        <div id="metric-fields"></div>
      </div>

      <!-- Budget Summary -->
      <div class="section-block" id="section-budget" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconChart}</span>
          <span class="section-title">Budget Summary</span>
        </div>
        <div id="budget-summary-fields"></div>
        <div class="table-wrap" style="margin-top:0.75rem;" id="budget-rows-wrap"></div>
      </div>

      <!-- Attachment Summary -->
      <div class="section-block" id="section-attachments" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconPaperclip}</span>
          <span class="section-title">Attachment Summary</span>
        </div>
        <div id="attachment-fields"></div>
      </div>

      <!-- Generated Reports -->
      <div class="section-block" id="section-reports" ${!sessionId ? 'style="display:none"' : ''}>
        <div class="section-header">
          <span class="section-icon">${iconFolder}</span>
          <span class="section-title">Generated Reports</span>
        </div>
        <div id="report-results"></div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost"    id="prev-back"         type="button">${iconArrowLeft}&nbsp; Dashboard</button>
      <button class="btn"              id="prev-gen-handover"  type="button" ${!sessionId ? 'disabled' : ''}>${iconSave}&nbsp; Handover Report</button>
      <button class="btn"              id="prev-gen-budget"    type="button" ${!sessionId ? 'disabled' : ''}>${iconChart}&nbsp; Budget Report</button>
      <button class="btn"              id="prev-gen-both"      type="button" ${!sessionId ? 'disabled' : ''}>${iconRefresh}&nbsp; Both Reports</button>
      <button class="btn btn-ghost"    id="prev-open-folder"   type="button" ${!sessionId ? 'disabled' : ''}>${iconExternalLink}&nbsp; Open Folder</button>
      <button class="btn btn-primary"  id="prev-go-send"       type="button" ${!sessionId ? 'disabled' : ''}>${iconSend}&nbsp; Send</button>
    </footer>
  `;

  root.append(screen);

  const msg             = screen.querySelector('#prev-message');
  const sessionFields   = screen.querySelector('#session-fields');
  const deptsCompleted  = screen.querySelector('#depts-completed-fields');
  const deptRows        = screen.querySelector('#dept-rows');
  const metricFields    = screen.querySelector('#metric-fields');
  const budgetFields    = screen.querySelector('#budget-summary-fields');
  const budgetRowsWrap  = screen.querySelector('#budget-rows-wrap');
  const attachFields    = screen.querySelector('#attachment-fields');
  const reportResults   = screen.querySelector('#report-results');

  const goBack = () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  screen.querySelector('#prev-back')?.addEventListener('click', goBack);
  screen.querySelector('#prev-back-hdr')?.addEventListener('click', goBack);
  screen.querySelector('#prev-go-send')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'send' } }));
  });

  if (!sessionId) return;

  const setBusy = (busy) => {
    ['#prev-gen-handover','#prev-gen-budget','#prev-gen-both','#prev-open-folder'].forEach((id) => {
      const el = screen.querySelector(id);
      if (el) el.disabled = busy;
    });
  };

  /* ── render helpers ── */
  function renderSessionFields(session) {
    sessionFields.innerHTML =
      fieldRow('Session #', session.sessionId) +
      fieldRow('Shift', `${session.shiftCode || '—'} · ${session.shiftDate || '—'}`) +
      fieldRow('Status', session.sessionStatus || 'Open') +
      fieldRow('Created', `${session.createdAt || '—'} by ${session.createdBy || '—'}`) +
      fieldRow('Updated', `${session.updatedAt || '—'} by ${session.updatedBy || '—'}`);
  }

  function renderDeptsCompleted(departments) {
    const all = Array.isArray(departments) ? departments : [];
    let complete = 0, incomplete = 0, notRunning = 0;
    all.forEach((d) => {
      const s = (d.deptStatus || '').toLowerCase().replace(/\s+/g, '');
      if (s === 'complete')    complete++;
      else if (s === 'incomplete') incomplete++;
      else notRunning++;
    });
    deptsCompleted.innerHTML =
      fieldRow('Total Departments', all.length) +
      fieldRow('Complete', complete, 'value-green') +
      fieldRow('Incomplete', incomplete, 'value-orange') +
      fieldRow('Not Running', notRunning, 'value-muted') +
      fieldRow('Completion', `${complete} / ${all.length} departments complete`);
  }

  function renderDeptRows(departments) {
    deptRows.innerHTML = '';
    if (!Array.isArray(departments) || !departments.length) {
      deptRows.innerHTML = '<p class="status-line">No department rows found.</p>';
      return;
    }
    departments.forEach((dept) => {
      const isMetric = metricDepartments.includes(dept.deptName);
      const row = document.createElement('div');
      row.style.cssText = 'padding:0.6rem 0;border-bottom:1px solid var(--border);';
      row.innerHTML = `
        <div style="display:flex;align-items:center;gap:0.5rem;margin-bottom:0.3rem;">
          <strong style="font-size:0.9rem;">${dept.deptName}</strong>
          <span class="${statusCls(dept.deptStatus)}" style="font-size:0.8rem;">${statusIcon(dept.deptStatus)} ${dept.deptStatus || '—'}</span>
          ${isMetric ? `<span style="font-size:0.72rem;color:var(--muted);margin-left:auto;">Metric</span>` : ''}
        </div>
        ${isMetric ? `<div style="display:flex;gap:1.5rem;font-size:0.8rem;color:var(--muted);margin-bottom:0.25rem;">
          <span>Downtime: <strong class="value-green">${dept.downtimeMin ?? '—'} min</strong></span>
          <span>Efficiency: <strong class="value-green">${fmt(dept.efficiencyPct, '%')}</strong></span>
          <span>Yield: <strong class="value-green">${fmt(dept.yieldPct, '%')}</strong></span>
        </div>` : ''}
        <div style="font-size:0.82rem;color:var(--muted);">
          ${dept.notes || dept.deptNotes ? `<span>Notes: ${dept.notes || dept.deptNotes}</span> · ` : ''}
          <span>Attachments: ${Number(dept.attachmentCount || 0)}</span>
          · <span>Updated: ${dept.updatedAt || 'n/a'}</span>
        </div>
      `;
      deptRows.append(row);
    });
  }

  function renderMetricSummary(departments) {
    const metric = (Array.isArray(departments) ? departments : [])
      .filter((d) => metricDepartments.includes(d.deptName));
    const totalDT    = metric.reduce((s, d) => s + (Number(d.downtimeMin) || 0), 0);
    const effVals    = metric.map((d) => d.efficiencyPct).filter((v) => v != null && v !== '');
    const yieldVals  = metric.map((d) => d.yieldPct).filter((v) => v != null && v !== '');
    const avgEff     = effVals.length   ? effVals.reduce((a, b) => a + Number(b), 0) / effVals.length   : null;
    const avgYield   = yieldVals.length ? yieldVals.reduce((a, b) => a + Number(b), 0) / yieldVals.length : null;
    metricFields.innerHTML =
      fieldRow('Total Downtime', `${totalDT} min`, 'value-green') +
      fieldRow('Average Efficiency', avgEff != null ? `${avgEff.toFixed(1)}%` : '—', 'value-green') +
      fieldRow('Average Yield',      avgYield != null ? `${avgYield.toFixed(1)}%` : '—', 'value-green');
  }

  function renderBudgetSummary(summary, rows) {
    const status = summary?.status || 'Not set';
    budgetFields.innerHTML =
      fieldRow('Planned Total', fmt(summary?.plannedTotal)) +
      fieldRow('Used Total', fmt(summary?.usedTotal)) +
      fieldRow('Variance Total', fmt(summary?.varianceTotal)) +
      fieldRow('Status', status, budgetStatusCls(status)) +
      fieldRow('Last Updated', `${summary?.lastUpdatedAt || '—'} by ${summary?.lastUpdatedBy || '—'}`);

    if (!Array.isArray(rows) || !rows.length) {
      budgetRowsWrap.innerHTML = '<p class="status-line">No budget rows saved.</p>';
      return;
    }
    budgetRowsWrap.innerHTML = `
      <table>
        <thead><tr>
          <th>Department</th><th>Planned</th><th>Used</th><th>Variance</th><th>Status</th><th>Reason</th>
        </tr></thead>
        <tbody>${rows.map((r) => `<tr>
          <td>${r.deptName || ''}</td>
          <td>${r.plannedQty ?? '—'}</td>
          <td>${r.usedQty ?? '—'}</td>
          <td>${r.variance != null ? (Number(r.variance) > 0 ? '+' : '') + fmt(r.variance) : '—'}</td>
          <td>${r.status || 'Not set'}</td>
          <td style="font-size:0.82rem;color:var(--muted);">${r.reasonText || ''}</td>
        </tr>`).join('')}</tbody>
      </table>`;
  }

  function renderAttachmentSummary(summary) {
    attachFields.innerHTML = '';
    if (!Array.isArray(summary) || !summary.length) {
      attachFields.innerHTML = '<p class="status-line">No attachments saved for this session.</p>';
      return;
    }
    summary.forEach((group) => {
      const div = document.createElement('div');
      div.style.cssText = 'padding:0.4rem 0;border-bottom:1px solid var(--border);font-size:0.85rem;';
      const names = Array.isArray(group.attachments) ? group.attachments.map((a) => a.displayName).join(', ') : '';
      div.innerHTML = `<strong>${group.deptName}</strong> — ${Number(group.attachmentCount || 0)} file(s)
        <span style="color:var(--muted);margin-left:0.5rem;">${names}</span>`;
      attachFields.append(div);
    });
  }

  function renderReportResults(reports) {
    reportResults.innerHTML = '';
    if (!Array.isArray(reports) || !reports.length) {
      reportResults.innerHTML = '<p class="status-line">No reports generated yet this session.</p>';
      return;
    }
    reports.forEach((r) => {
      const div = document.createElement('div');
      div.style.cssText = 'padding:0.4rem 0;border-bottom:1px solid var(--border);font-size:0.85rem;';
      div.innerHTML = `<strong>${r.reportType || 'report'}</strong> — ${r.generatedAt || '—'} by ${r.generatedBy || '—'}
        ${(Array.isArray(r.filePaths) ? r.filePaths : []).map((p) => `<div style="color:var(--muted);font-size:0.78rem;">${p}</div>`).join('')}`;
      reportResults.append(div);
    });
  }

  /* ── load preview ── */
  const loadPreview = async () => {
    msg.textContent = 'Loading saved state…';
    msg.className   = 'status-line';
    try {
      const preview = await previewService.loadPreview(sessionId);
      applyPreviewPayload(preview);
      renderSessionFields(preview.session || {});
      renderDeptsCompleted(preview.departments || []);
      renderDeptRows(preview.departments || []);
      renderMetricSummary(preview.departments || []);
      renderBudgetSummary(preview.budgetSummary || {}, preview.budgetRows || []);
      renderAttachmentSummary(preview.attachmentSummary || []);
      renderReportResults(state.generatedReports || []);
      msg.textContent = 'Preview loaded from saved state.';
      msg.className   = 'status-line success';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to load preview.';
      msg.className   = 'status-line error';
    }
  };

  const runReport = async (runner, label) => {
    setBusy(true);
    msg.textContent = `${label}…`;
    msg.className   = 'status-line';
    try {
      const result = await runner();
      appendGeneratedReport(result);
      renderReportResults(state.generatedReports || []);
      msg.textContent = `${label} complete.`;
      msg.className   = 'status-line success';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : `${label} failed.`;
      msg.className   = 'status-line error';
    } finally {
      setBusy(false);
    }
  };

  screen.querySelector('#prev-gen-handover')?.addEventListener('click', () =>
    runReport(() => reportsService.generateHandoverReport(sessionId, state.session?.userName || ''), 'Handover report'));
  screen.querySelector('#prev-gen-budget')?.addEventListener('click', () =>
    runReport(() => reportsService.generateBudgetReport(sessionId, state.session?.userName || ''), 'Budget report'));
  screen.querySelector('#prev-gen-both')?.addEventListener('click', () =>
    runReport(() => reportsService.generateAllReports(sessionId, state.session?.userName || ''), 'Both reports'));

  screen.querySelector('#prev-open-folder')?.addEventListener('click', async () => {
    setBusy(true);
    msg.textContent = 'Opening reports folder…';
    try {
      const result = await reportsService.openReportsFolder(sessionId);
      msg.textContent = `Opened: ${result.openedPath || 'n/a'}`;
      msg.className   = 'status-line success';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to open folder.';
      msg.className   = 'status-line error';
    } finally { setBusy(false); }
  });

  loadPreview();
}
