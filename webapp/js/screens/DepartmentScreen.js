import { departmentsService } from '../services/departmentsService.js';
import { attachmentsService } from '../services/attachmentsService.js';
import {
  applyActiveDepartmentPayload,
  applyAttachmentListPayload,
  applyDepartmentSummaryPayload,
  setActiveDepartmentName,
  setSelectedAttachmentId
} from '../state/appState.js';

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function optionMarkup(departments, selectedDeptName) {
  return departments
    .map((dept) => {
      const safeDeptName = escapeHtml(dept.deptName);
      const selected = dept.deptName === selectedDeptName ? 'selected' : '';
      return `<option value="${safeDeptName}" ${selected}>${safeDeptName}</option>`;
    })
    .join('');
}

function metricFieldsMarkup(model) {
  if (!model?.isMetricDept) {
    return '<p class="meta">This is a non-metric department. Downtime, efficiency, and yield are not used.</p>';
  }

  return `
    <label>
      Downtime (min)
      <input type="number" min="0" step="1" name="downtimeMin" value="${escapeHtml(model.downtimeMin ?? '')}" />
    </label>
    <label>
      Efficiency (%)
      <input type="number" min="0" max="100" step="0.1" name="efficiencyPct" value="${escapeHtml(model.efficiencyPct ?? '')}" />
    </label>
    <label>
      Yield (%)
      <input type="number" min="0" max="100" step="0.1" name="yieldPct" value="${escapeHtml(model.yieldPct ?? '')}" />
    </label>
  `;
}

function toNumberOrNull(value) {
  if (value === '' || value == null) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function attachmentListMarkup(attachments, selectedId) {
  if (!attachments?.length) {
    return '<li class="meta">No attachments saved for this department.</li>';
  }

  return attachments
    .map((item) => {
      const selectedClass = item.attachmentId === selectedId ? 'attachment-selected' : '';
      return `<li>
        <button type="button" class="secondary attachment-item ${selectedClass}" data-attachment-id="${escapeHtml(item.attachmentId)}">
          #${escapeHtml(item.sequenceNo)} — ${escapeHtml(item.displayName)}
        </button>
      </li>`;
    })
    .join('');
}

function selectedAttachmentMarkup(attachments, selectedId) {
  const selected = attachments.find((item) => item.attachmentId === selectedId) || attachments[0];
  if (!selected) {
    return '<p class="meta">Select an attachment to view metadata.</p>';
  }

  return `
    <p><strong>${escapeHtml(selected.displayName)}</strong></p>
    <p class="meta">Captured: ${escapeHtml(selected.capturedOn || 'n/a')}</p>
    <p class="meta">Stored path: ${escapeHtml(selected.filePath)}</p>
  `;
}

export function renderDepartmentScreen(root, state) {
  const session = state.session;
  if (!session?.sessionId) {
    root.innerHTML = '<section class="panel"><h2>Department</h2><p class="meta">Open a session first.</p></section>';
    return;
  }

  const deptOptions = session.departments || [];
  const selectedDeptName = state.activeDepartmentName || deptOptions[0]?.deptName;
  if (!selectedDeptName) {
    root.innerHTML = '<section class="panel"><h2>Department</h2><p class="meta">No departments available in this session.</p></section>';
    return;
  }

  setActiveDepartmentName(selectedDeptName);

  root.innerHTML = `
    <section class="panel department-layout">
      <h2>Department</h2>
      <p class="meta">Session #${escapeHtml(session.sessionId)} • ${escapeHtml(session.shiftCode)} • ${escapeHtml(session.shiftDate)}</p>
      <label>
        Department
        <select id="dept-selector">${optionMarkup(deptOptions, selectedDeptName)}</select>
      </label>
      <p id="dept-message" class="meta">Loading persisted department values...</p>
      <div id="dept-form-area"></div>

      <h3>Attachments</h3>
      <p id="attachment-message" class="meta">Loading attachments...</p>
      <p class="meta">Attachment count: <span id="attachment-count">0</span></p>
      <ul id="attachment-list" class="attachment-list"></ul>
      <div id="attachment-selected-meta"></div>
      <div class="actions-row">
        <button id="attachment-add" type="button">Add Attachment</button>
        <button id="attachment-remove" type="button" class="secondary">Remove Selected</button>
        <button id="attachment-open-viewer" type="button" class="secondary">Open Viewer</button>
      </div>

      <div class="actions-row">
        <button id="dept-back" class="secondary" type="button">Back to Dashboard</button>
      </div>
    </section>
  `;

  const selector = root.querySelector('#dept-selector');
  const message = root.querySelector('#dept-message');
  const formArea = root.querySelector('#dept-form-area');
  const attachmentMessage = root.querySelector('#attachment-message');
  const attachmentCount = root.querySelector('#attachment-count');
  const attachmentList = root.querySelector('#attachment-list');
  const attachmentSelectedMeta = root.querySelector('#attachment-selected-meta');
  const addButton = root.querySelector('#attachment-add');
  const removeButton = root.querySelector('#attachment-remove');
  const openViewerButton = root.querySelector('#attachment-open-viewer');

  function refreshAttachmentPanel() {
    const attachments = state.activeAttachments || [];
    attachmentCount.textContent = String(attachments.length);
    attachmentList.innerHTML = attachmentListMarkup(attachments, state.selectedAttachmentId);
    attachmentSelectedMeta.innerHTML = selectedAttachmentMarkup(attachments, state.selectedAttachmentId);

    attachmentList.querySelectorAll('[data-attachment-id]').forEach((button) => {
      button.addEventListener('click', () => {
        const selectedId = Number(button.getAttribute('data-attachment-id'));
        setSelectedAttachmentId(selectedId);
        refreshAttachmentPanel();
      });
    });
  }

  async function loadAttachments() {
    const activeDepartment = state.activeDepartment;
    if (!activeDepartment?.deptRecordId) {
      applyAttachmentListPayload({ attachments: [] });
      refreshAttachmentPanel();
      return;
    }

    attachmentMessage.textContent = `Loading attachments for ${activeDepartment.deptName}...`;
    try {
      const payload = await attachmentsService.listAttachments(session.sessionId, activeDepartment.deptRecordId, activeDepartment.deptName);
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachmentMessage.textContent = `Loaded ${payload.attachmentCount} attachment(s).`;
      refreshAttachmentPanel();
    } catch (error) {
      attachmentMessage.textContent = error instanceof Error ? error.message : 'Failed to load attachments.';
    }
  }

  async function loadSelectedDepartment() {
    const deptName = selector.value;
    setActiveDepartmentName(deptName);
    message.textContent = `Loading ${deptName}...`;

    try {
      const department = await departmentsService.loadDepartment(session.sessionId, deptName);
      applyActiveDepartmentPayload(department);
      renderEditForm(department);
      message.textContent = `Loaded ${deptName}.`;
      await loadAttachments();
    } catch (error) {
      formArea.innerHTML = '';
      message.textContent = error instanceof Error ? error.message : 'Failed to load department.';
      attachmentMessage.textContent = 'Attachment list unavailable until department loads.';
    }
  }

  function renderEditForm(model) {
    formArea.innerHTML = `
      <form id="dept-form" class="form-grid">
        <label>
          Status
          <input type="text" name="deptStatus" required value="${escapeHtml(model.deptStatus || 'Not running')}" />
        </label>
        ${metricFieldsMarkup(model)}
        <label>
          Notes
          <textarea name="deptNotes" rows="5">${escapeHtml(model.deptNotes || '')}</textarea>
        </label>
        <div class="actions-row">
          <button type="submit">Save Department</button>
          <button id="save-return" type="button" class="secondary">Save + Return</button>
        </div>
      </form>
    `;

    const form = root.querySelector('#dept-form');
    const saveReturn = root.querySelector('#save-return');

    const performSave = async (goDashboard) => {
      const data = new FormData(form);
      const payload = {
        deptRecordId: model.deptRecordId,
        sessionId: session.sessionId,
        deptName: model.deptName,
        deptStatus: String(data.get('deptStatus') || '').trim() || 'Not running',
        deptNotes: String(data.get('deptNotes') || ''),
        downtimeMin: toNumberOrNull(data.get('downtimeMin')),
        efficiencyPct: toNumberOrNull(data.get('efficiencyPct')),
        yieldPct: toNumberOrNull(data.get('yieldPct')),
        userName: session.userName || ''
      };

      const validation = departmentsService.validateDepartment(payload);
      if (!validation.ok) {
        message.textContent = validation.errors.join(' ');
        return;
      }

      message.textContent = `Saving ${payload.deptName}...`;
      try {
        const saveResult = await departmentsService.saveDepartment(payload);
        applyActiveDepartmentPayload(saveResult.department);
        applyDepartmentSummaryPayload(saveResult.dashboardDepartments);
        selector.value = saveResult.department.deptName;
        renderEditForm(saveResult.department);
        message.textContent = `Saved ${payload.deptName} at ${saveResult.department.updatedAt || 'now'}.`;

        if (goDashboard) {
          window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
        }
      } catch (error) {
        message.textContent = error instanceof Error ? error.message : 'Failed to save department.';
      }
    };

    form.addEventListener('submit', async (event) => {
      event.preventDefault();
      await performSave(false);
    });

    saveReturn.addEventListener('click', async () => {
      await performSave(true);
    });
  }

  selector.addEventListener('change', async () => {
    await loadSelectedDepartment();
  });

  addButton?.addEventListener('click', async () => {
    const activeDepartment = state.activeDepartment;
    if (!activeDepartment?.deptRecordId) {
      attachmentMessage.textContent = 'Load a department first.';
      return;
    }

    try {
      const picked = await attachmentsService.pickFile();
      if (!picked?.picked || !picked.sourcePath) {
        attachmentMessage.textContent = 'Attachment selection cancelled.';
        return;
      }

      attachmentMessage.textContent = 'Copying selected file and saving metadata...';
      const payload = await attachmentsService.addAttachment(
        session.sessionId,
        activeDepartment.deptRecordId,
        activeDepartment.deptName,
        picked.sourcePath,
        picked.displayName || '',
        session.userName || ''
      );
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachmentMessage.textContent = `Added attachment: ${picked.displayName || 'file'}.`;
      refreshAttachmentPanel();
    } catch (error) {
      attachmentMessage.textContent = error instanceof Error ? error.message : 'Failed to add attachment.';
    }
  });

  removeButton?.addEventListener('click', async () => {
    const selectedAttachmentId = state.selectedAttachmentId;
    if (!selectedAttachmentId) {
      attachmentMessage.textContent = 'Select an attachment first.';
      return;
    }

    try {
      attachmentMessage.textContent = 'Removing attachment metadata...';
      const payload = await attachmentsService.removeAttachment(selectedAttachmentId, session.userName || '');
      applyAttachmentListPayload(payload);
      applyDepartmentSummaryPayload(payload.dashboardDepartments);
      attachmentMessage.textContent = 'Attachment removed.';
      refreshAttachmentPanel();
    } catch (error) {
      attachmentMessage.textContent = error instanceof Error ? error.message : 'Failed to remove attachment.';
    }
  });

  openViewerButton?.addEventListener('click', async () => {
    const activeDepartment = state.activeDepartment;
    const selectedAttachmentId = state.selectedAttachmentId;
    if (!activeDepartment?.deptRecordId || !selectedAttachmentId) {
      attachmentMessage.textContent = 'Select an attachment to open viewer.';
      return;
    }

    window.dispatchEvent(
      new CustomEvent('app:navigate', {
        detail: {
          route: 'viewer',
          deptName: activeDepartment.deptName,
          attachmentId: selectedAttachmentId
        }
      })
    );
  });

  root.querySelector('#dept-back')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  void loadSelectedDepartment();
}
