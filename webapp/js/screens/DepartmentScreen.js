import { departmentsService } from '../services/departmentsService.js';
import { attachmentsService } from '../services/attachmentsService.js';
import {
  applyActiveDepartmentPayload,
  applyAttachmentListPayload,
  applyDepartmentSummaryPayload,
  setActiveDepartmentName,
  setSelectedAttachmentId
} from '../state/appState.js';

function toNumberOrNull(value) {
  if (value === '' || value == null) {
    return null;
  }

  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

function createDepartmentOptions(select, departments, selectedDeptName) {
  select.textContent = '';
  departments.forEach((dept) => {
    const option = document.createElement('option');
    option.value = dept.deptName;
    option.textContent = dept.deptName;
    option.selected = dept.deptName === selectedDeptName;
    select.append(option);
  });
}

function createAttachmentList(listEl, attachments, selectedId, onSelect) {
  listEl.textContent = '';

  if (!attachments.length) {
    const empty = document.createElement('li');
    empty.className = 'meta';
    empty.textContent = 'No attachments saved for this department.';
    listEl.append(empty);
    return;
  }

  attachments.forEach((item) => {
    const li = document.createElement('li');
    const button = document.createElement('button');
    button.type = 'button';
    button.className = 'secondary attachment-item';
    if (item.attachmentId === selectedId) {
      button.classList.add('attachment-selected');
    }

    button.textContent = `#${item.sequenceNo} — ${item.displayName}`;
    button.addEventListener('click', () => onSelect(item.attachmentId));
    li.append(button);
    listEl.append(li);
  });
}

function renderSelectedAttachmentMeta(container, attachments, selectedId) {
  container.textContent = '';

  const selected = attachments.find((item) => item.attachmentId === selectedId) || attachments[0];
  if (!selected) {
    const none = document.createElement('p');
    none.className = 'meta';
    none.textContent = 'Select an attachment to view metadata.';
    container.append(none);
    return;
  }

  const name = document.createElement('p');
  const strong = document.createElement('strong');
  strong.textContent = selected.displayName;
  name.append(strong);

  const captured = document.createElement('p');
  captured.className = 'meta';
  captured.textContent = `Captured: ${selected.capturedOn || 'n/a'}`;

  const path = document.createElement('p');
  path.className = 'meta';
  path.textContent = `Stored path: ${selected.filePath}`;

  container.append(name, captured, path);
}

function renderDepartmentForm(formArea, model) {
  formArea.textContent = '';

  const form = document.createElement('form');
  form.id = 'dept-form';
  form.className = 'form-grid';

  const statusLabel = document.createElement('label');
  statusLabel.append('Status');
  const statusInput = document.createElement('input');
  statusInput.type = 'text';
  statusInput.name = 'deptStatus';
  statusInput.required = true;
  statusInput.value = model.deptStatus || 'Not running';
  statusLabel.append(statusInput);
  form.append(statusLabel);

  if (model.isMetricDept) {
    const dtLabel = document.createElement('label');
    dtLabel.append('Downtime (min)');
    const dtInput = document.createElement('input');
    dtInput.type = 'number';
    dtInput.min = '0';
    dtInput.step = '1';
    dtInput.name = 'downtimeMin';
    dtInput.value = model.downtimeMin ?? '';
    dtLabel.append(dtInput);

    const effLabel = document.createElement('label');
    effLabel.append('Efficiency (%)');
    const effInput = document.createElement('input');
    effInput.type = 'number';
    effInput.min = '0';
    effInput.max = '100';
    effInput.step = '0.1';
    effInput.name = 'efficiencyPct';
    effInput.value = model.efficiencyPct ?? '';
    effLabel.append(effInput);

    const yieldLabel = document.createElement('label');
    yieldLabel.append('Yield (%)');
    const yieldInput = document.createElement('input');
    yieldInput.type = 'number';
    yieldInput.min = '0';
    yieldInput.max = '100';
    yieldInput.step = '0.1';
    yieldInput.name = 'yieldPct';
    yieldInput.value = model.yieldPct ?? '';
    yieldLabel.append(yieldInput);

    form.append(dtLabel, effLabel, yieldLabel);
  } else {
    const note = document.createElement('p');
    note.className = 'meta';
    note.textContent = 'This is a non-metric department. Downtime, efficiency, and yield are not used.';
    form.append(note);
  }

  const notesLabel = document.createElement('label');
  notesLabel.append('Notes');
  const notesArea = document.createElement('textarea');
  notesArea.name = 'deptNotes';
  notesArea.rows = 5;
  notesArea.value = model.deptNotes || '';
  notesLabel.append(notesArea);
  form.append(notesLabel);

  const actions = document.createElement('div');
  actions.className = 'actions-row';

  const saveButton = document.createElement('button');
  saveButton.type = 'submit';
  saveButton.textContent = 'Save Department';

  const saveReturn = document.createElement('button');
  saveReturn.id = 'save-return';
  saveReturn.type = 'button';
  saveReturn.className = 'secondary';
  saveReturn.textContent = 'Save + Return';

  actions.append(saveButton, saveReturn);
  form.append(actions);

  formArea.append(form);

  return { form, saveReturn };
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
      <p id="dept-session" class="meta"></p>
      <label>
        Department
        <select id="dept-selector"></select>
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

  root.querySelector('#dept-session').textContent = `Session #${session.sessionId} • ${session.shiftCode} • ${session.shiftDate}`;

  const selector = root.querySelector('#dept-selector');
  createDepartmentOptions(selector, deptOptions, selectedDeptName);

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

    createAttachmentList(attachmentList, attachments, state.selectedAttachmentId, (selectedId) => {
      setSelectedAttachmentId(selectedId);
      refreshAttachmentPanel();
    });

    renderSelectedAttachmentMeta(attachmentSelectedMeta, attachments, state.selectedAttachmentId);
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
      bindDepartmentForm(department);
      message.textContent = `Loaded ${deptName}.`;
      await loadAttachments();
    } catch (error) {
      formArea.textContent = '';
      message.textContent = error instanceof Error ? error.message : 'Failed to load department.';
      attachmentMessage.textContent = 'Attachment list unavailable until department loads.';
    }
  }

  function bindDepartmentForm(model) {
    const { form, saveReturn } = renderDepartmentForm(formArea, model);

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
        bindDepartmentForm(saveResult.department);
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
