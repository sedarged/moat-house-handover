import { departmentsService } from '../services/departmentsService.js';
import { attachmentsService  } from '../services/attachmentsService.js';
import {
  applyActiveDepartmentPayload,
  applyAttachmentListPayload,
  applyDepartmentSummaryPayload,
  setActiveDepartmentName,
  setSelectedAttachmentId
} from '../state/appState.js';
import { DEPARTMENTS, DEPT_STATUSES, createDepartmentModel } from '../models/contracts.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar,
  iconClockSm, iconSave, iconPlus, iconTrash, iconExternalLink, iconPrev, iconNext
} from '../core/icons.js';

function toNumberOrNull(value) {
  if (value === '' || value == null) return null;
  const n = Number(value);
  return Number.isNaN(n) ? null : n;
}

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function statusClass(s) {
  const lc = (s || '').toLowerCase().replace(/\s+/g, '');
  if (lc === 'complete')   return 'status-select-complete';
  if (lc === 'incomplete') return 'status-select-incomplete';
  return 'status-select-notrunning';
}

export function renderDepartmentScreen(root, state) {
  const session = state.session;
  if (!session?.sessionId) {
    root.innerHTML = `<div class="screen"><div class="screen-content" style="display:flex;align-items:center;justify-content:center;"><p class="status-line">Open a session first.</p></div></div>`;
    return;
  }

  const deptOptions      = session.departments || [];
  const selectedDeptName = state.activeDepartmentName || deptOptions[0]?.deptName || DEPARTMENTS[0];

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="dept-back-hdr" title="Back to Dashboard" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">DEPARTMENT HANDOVER</div>
      <div class="header-actions">
        <button class="header-icon-btn" type="button">${iconBell}</button>
        <span class="header-divider"></span>
        <button class="header-icon-btn" type="button">${iconUser}</button>
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
        <span class="infobar-icon">${iconUser}</span>
        <span>Session #${session.sessionId}</span>
      </div>
      <div class="infobar-item" id="dept-metric-badge"></div>
    </div>

    <div class="screen-content">
      <div style="display:grid;grid-template-columns:1fr 300px;gap:1rem;align-items:start;">

        <div>
          <div class="section-block" style="margin-bottom:0.75rem;">
            <div class="form-field" style="margin-bottom:0.75rem;">
              <label class="form-label">Department</label>
              <select id="dept-selector" style="font-weight:600;font-size:1rem;"></select>
            </div>
            <p class="status-line" id="dept-message">Loading…</p>
          </div>
          <div class="section-block" id="dept-form-area"></div>
        </div>

        <div class="section-block" style="margin-bottom:0;">
          <div class="section-header">
            <span class="section-title">Attachments</span>
            <span style="margin-left:auto;font-size:0.78rem;color:var(--muted);">
              <span id="attach-count">0</span> file(s)
            </span>
          </div>
          <p class="status-line" id="attach-message" style="margin-bottom:0.5rem;"></p>
          <div class="attachment-panel">
            <ul class="attachment-list" id="attach-list"></ul>
          </div>
          <div class="attachment-meta" id="attach-meta">Select an attachment to view details.</div>
          <div class="actions-row" style="margin-top:0.75rem;">
            <button class="btn" id="attach-add" type="button">${iconPlus} Add</button>
            <button class="btn btn-ghost" id="attach-remove" type="button" title="Remove Selected">${iconTrash}</button>
            <button class="btn btn-ghost" id="attach-viewer" type="button" title="Open Viewer">${iconExternalLink}</button>
          </div>
          <div class="actions-row" style="margin-top:0.5rem;">
            <button class="btn btn-ghost" id="attach-prev" type="button">${iconPrev} Prev</button>
            <button class="btn btn-ghost" id="attach-next" type="button">Next ${iconNext}</button>
          </div>
        </div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost" id="dept-back-footer" type="button">${iconArrowLeft}&nbsp; Dashboard</button>
      <button class="btn btn-primary" id="dept-save-return" type="button">${iconSave}&nbsp; Save &amp; Return</button>
      <button class="btn" id="dept-save" type="button">${iconSave}&nbsp; Save</button>
    </footer>
  `;

  root.append(screen);

  const selector    = screen.querySelector('#dept-selector');
  const metricBadge = screen.querySelector('#dept-metric-badge');
  const deptMessage = screen.querySelector('#dept-message');
  const formArea    = screen.querySelector('#dept-form-area');
  const attachMsg   = screen.querySelector('#attach-message');
  const attachCount = screen.querySelector('#attach-count');
  const attachList  = screen.querySelector('#attach-list');
  const attachMeta  = screen.querySelector('#attach-meta');
  const addBtn      = screen.querySelector('#attach-add');
  const removeBtn   = screen.querySelector('#attach-remove');
  const viewerBtn   = screen.querySelector('#attach-viewer');
  const prevBtn     = screen.querySelector('#attach-prev');
  const nextBtn     = screen.querySelector('#attach-next');

  /* populate dept selector from full v2 list */
  DEPARTMENTS.forEach((name) => {
    const opt = document.createElement('option');
    opt.value = name;
    opt.textContent = name;
    opt.selected = name === selectedDeptName;
    selector.append(opt);
  });

  const goBack = () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  screen.querySelector('#dept-back-hdr')?.addEventListener('click', goBack);
  screen.querySelector('#dept-back-footer')?.addEventListener('click', goBack);

  /* attachment panel */
  function refreshAttachPanel() {
    const items = state.activeAttachments || [];
    attachCount.textContent = String(items.length);
    attachList.innerHTML = '';

    if (!items.length) {
      const li = document.createElement('li');
      li.className = 'attachment-list-item';
      li.style.color = 'var(--muted)';
      li.textContent = 'No attachments saved.';
      attachList.append(li);
    } else {
      items.forEach((item) => {
        const li = document.createElement('li');
        li.className = 'attachment-list-item' + (item.attachmentId === state.selectedAttachmentId ? ' selected' : '');
        li.textContent = `#${item.sequenceNo} — ${item.displayName}`;
        li.addEventListener('click', () => { setSelectedAttachmentId(item.attachmentId); refreshAttachPanel(); });
        attachList.append(li);
      });
    }

    const sel = items.find((a) => a.attachmentId === state.selectedAttachmentId) || items[0];
    if (sel) {
      attachMeta.innerHTML = `<strong>${sel.displayName}</strong><br>
        <span style="color:var(--muted)">Captured: ${sel.capturedOn || 'n/a'}</span>`;
    } else {
      attachMeta.textContent = 'Select an attachment to view details.';
    }

    const idx = items.findIndex((a) => a.attachmentId === state.selectedAttachmentId);
    prevBtn.disabled = idx <= 0;
    nextBtn.disabled = idx < 0 || idx >= items.length - 1;
  }

  prevBtn.addEventListener('click', () => {
    const items = state.activeAttachments || [];
    const idx   = items.findIndex((a) => a.attachmentId === state.selectedAttachmentId);
    if (idx > 0) { setSelectedAttachmentId(items[idx - 1].attachmentId); refreshAttachPanel(); }
  });
  nextBtn.addEventListener('click', () => {
    const items = state.activeAttachments || [];
    const idx   = items.findIndex((a) => a.attachmentId === state.selectedAttachmentId);
    if (idx < items.length - 1) { setSelectedAttachmentId(items[idx + 1].attachmentId); refreshAttachPanel(); }
  });

  async function loadAttachments() {
    const dept = state.activeDepartment;
    if (!dept?.deptRecordId) { applyAttachmentListPayload({ attachments: [] }); refreshAttachPanel(); return; }
    attachMsg.textContent = 'Loading attachments…';
    attachMsg.className   = 'status-line';
    try {
      const payload = await attachmentsService.listAttachments(session.sessionId, dept.deptRecordId, dept.deptName);
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachMsg.textContent = '';
      refreshAttachPanel();
    } catch (e) {
      attachMsg.textContent = e instanceof Error ? e.message : 'Failed to load attachments.';
      attachMsg.className   = 'status-line error';
    }
  }

  function renderForm(model) {
    formArea.innerHTML = '';
    const isMetric = model.isMetricDept;

    metricBadge.innerHTML = isMetric
      ? `<span style="font-size:0.76rem;background:rgba(234,88,12,0.15);color:#fb923c;padding:0.2rem 0.55rem;border-radius:4px;border:1px solid rgba(234,88,12,0.3);">Metric Dept</span>`
      : `<span style="font-size:0.76rem;background:var(--surface-2);color:var(--muted);padding:0.2rem 0.55rem;border-radius:4px;border:1px solid var(--border);">Non-metric</span>`;

    const form = document.createElement('form');
    form.id = 'dept-form';
    form.className = 'form-grid';

    /* Status */
    const statusWrap = document.createElement('div');
    statusWrap.className = 'form-field';
    statusWrap.innerHTML = `<label class="form-label">Status</label>`;
    const statusSel = document.createElement('select');
    statusSel.name = 'deptStatus';
    statusSel.required = true;
    DEPT_STATUSES.forEach((s) => {
      const opt = document.createElement('option');
      opt.value = s; opt.textContent = s;
      opt.selected = s === (model.deptStatus || 'Not running');
      statusSel.append(opt);
    });
    statusSel.className = statusClass(model.deptStatus);
    statusSel.addEventListener('change', () => { statusSel.className = statusClass(statusSel.value); });
    statusWrap.append(statusSel);
    form.append(statusWrap);

    /* Metric fields — only metric depts */
    if (isMetric) {
      const grid = document.createElement('div');
      grid.style.cssText = 'display:grid;grid-template-columns:repeat(3,1fr);gap:0.75rem;';
      grid.innerHTML = `
        <div class="form-field">
          <label class="form-label">Downtime (min)</label>
          <input type="number" name="downtimeMin" min="0" step="1" value="${model.downtimeMin ?? ''}" placeholder="0" />
        </div>
        <div class="form-field">
          <label class="form-label">Efficiency %</label>
          <input type="number" name="efficiencyPct" min="0" max="100" step="0.1" value="${model.efficiencyPct ?? ''}" placeholder="—" />
        </div>
        <div class="form-field">
          <label class="form-label">Yield %</label>
          <input type="number" name="yieldPct" min="0" max="100" step="0.1" value="${model.yieldPct ?? ''}" placeholder="—" />
        </div>
      `;
      form.append(grid);
    }

    /* Notes */
    const notesWrap = document.createElement('div');
    notesWrap.className = 'form-field';
    notesWrap.innerHTML = `<label class="form-label">Notes</label>`;
    const notesArea = document.createElement('textarea');
    notesArea.name = 'deptNotes'; notesArea.rows = 6;
    notesArea.value = model.deptNotes || '';
    notesArea.placeholder = 'Enter handover notes…';
    notesWrap.append(notesArea);
    form.append(notesWrap);

    formArea.append(form);

    /* save logic — rebind footer buttons each time form is rendered */
    const doSave = async (andReturn) => {
      const fd = new FormData(form);
      const payload = {
        deptRecordId:  model.deptRecordId,
        sessionId:     session.sessionId,
        deptName:      model.deptName,
        deptStatus:    String(fd.get('deptStatus') || '').trim() || 'Not running',
        deptNotes:     String(fd.get('deptNotes') || ''),
        downtimeMin:   isMetric ? toNumberOrNull(fd.get('downtimeMin'))   : null,
        efficiencyPct: isMetric ? toNumberOrNull(fd.get('efficiencyPct')) : null,
        yieldPct:      isMetric ? toNumberOrNull(fd.get('yieldPct'))      : null,
        userName:      session.userName || ''
      };

      const validation = departmentsService.validateDepartment(payload);
      if (!validation.ok) {
        deptMessage.textContent = validation.errors.join(' ');
        deptMessage.className   = 'status-line error';
        return;
      }

      deptMessage.textContent = `Saving ${payload.deptName}…`;
      deptMessage.className   = 'status-line';
      try {
        const result = await departmentsService.saveDepartment(payload);
        applyActiveDepartmentPayload(result.department);
        applyDepartmentSummaryPayload(result.dashboardDepartments);
        deptMessage.textContent = `Saved ${payload.deptName}.`;
        deptMessage.className   = 'status-line success';
        renderForm(result.department);
        if (andReturn) goBack();
      } catch (e) {
        deptMessage.textContent = e instanceof Error ? e.message : 'Save failed.';
        deptMessage.className   = 'status-line error';
      }
    };

    /* re-bind footer buttons */
    const saveBtn       = screen.querySelector('#dept-save');
    const saveReturnBtn = screen.querySelector('#dept-save-return');
    if (saveBtn) {
      saveBtn.onclick       = () => doSave(false);
    }
    if (saveReturnBtn) {
      saveReturnBtn.onclick = () => doSave(true);
    }
    form.addEventListener('submit', (e) => { e.preventDefault(); doSave(false); });
  }

  async function loadDept(deptName) {
    setActiveDepartmentName(deptName);
    deptMessage.textContent = `Loading ${deptName}…`;
    deptMessage.className   = 'status-line';
    formArea.innerHTML      = '';
    try {
      const dept = await departmentsService.loadDepartment(session.sessionId, deptName);
      applyActiveDepartmentPayload(dept);
      renderForm(dept);
      deptMessage.textContent = '';
      await loadAttachments();
    } catch (e) {
      /* fall back to a default form so the UI remains usable in dev/browser mode */
      const fallback = createDepartmentModel(deptName);
      applyActiveDepartmentPayload(fallback);
      renderForm(fallback);
      const errMsg = e instanceof Error ? e.message : 'Failed to load department.';
      deptMessage.textContent = errMsg;
      deptMessage.className   = 'status-line error';
      await loadAttachments();
    }
  }

  selector.addEventListener('change', () => loadDept(selector.value));

  /* attachment buttons */
  addBtn.addEventListener('click', async () => {
    const dept = state.activeDepartment;
    if (!dept?.deptRecordId) { attachMsg.textContent = 'Load a department first.'; return; }
    try {
      const picked = await attachmentsService.pickFile();
      if (!picked?.picked || !picked.sourcePath) { attachMsg.textContent = 'Cancelled.'; return; }
      attachMsg.textContent = 'Adding…';
      const payload = await attachmentsService.addAttachment(
        session.sessionId, dept.deptRecordId, dept.deptName,
        picked.sourcePath, picked.displayName || '', session.userName || ''
      );
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachMsg.textContent = `Added: ${picked.displayName || 'file'}.`;
      attachMsg.className   = 'status-line success';
      refreshAttachPanel();
    } catch (e) {
      attachMsg.textContent = e instanceof Error ? e.message : 'Failed to add.';
      attachMsg.className   = 'status-line error';
    }
  });

  removeBtn.addEventListener('click', async () => {
    if (!state.selectedAttachmentId) { attachMsg.textContent = 'Select an attachment first.'; return; }
    if (!window.confirm('Remove selected attachment?')) return;
    try {
      attachMsg.textContent = 'Removing…';
      const payload = await attachmentsService.removeAttachment(state.selectedAttachmentId, session.userName || '');
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachMsg.textContent = 'Removed.';
      attachMsg.className   = 'status-line success';
      refreshAttachPanel();
    } catch (e) {
      attachMsg.textContent = e instanceof Error ? e.message : 'Failed to remove.';
      attachMsg.className   = 'status-line error';
    }
  });

  viewerBtn.addEventListener('click', () => {
    const dept = state.activeDepartment;
    if (!dept?.deptRecordId || !state.selectedAttachmentId) {
      attachMsg.textContent = 'Select an attachment to open the viewer.';
      return;
    }
    window.dispatchEvent(new CustomEvent('app:navigate', {
      detail: { route: 'viewer', deptName: dept.deptName, attachmentId: state.selectedAttachmentId }
    }));
  });

  void loadDept(selectedDeptName);
}
