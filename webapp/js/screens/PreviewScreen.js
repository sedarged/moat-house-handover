import { previewService } from '../services/previewService.js';
import { reportsService } from '../services/reportsService.js';
import { applyPreviewPayload, appendGeneratedReport } from '../state/appState.js';

function formatNumber(value) {
  if (value == null || value === '') {
    return '—';
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed.toFixed(2) : '—';
}

function makeMeta(label, value) {
  const p = document.createElement('p');
  p.className = 'meta';
  p.textContent = `${label}: ${value || 'n/a'}`;
  return p;
}

function makeSection(title) {
  const section = document.createElement('section');
  section.className = 'panel';
  const heading = document.createElement('h3');
  heading.textContent = title;
  section.append(heading);
  return section;
}

function renderSessionHeader(container, session) {
  container.textContent = '';
  container.append(makeMeta('Session', String(session.sessionId || 'n/a')));
  container.append(makeMeta('Shift', `${session.shiftCode || 'n/a'} • ${session.shiftDate || 'n/a'}`));
  container.append(makeMeta('Status', session.sessionStatus || 'Open'));
  container.append(makeMeta('Created', `${session.createdAt || 'n/a'} by ${session.createdBy || 'n/a'}`));
  container.append(makeMeta('Updated', `${session.updatedAt || 'n/a'} by ${session.updatedBy || 'n/a'}`));
}

function renderDepartments(container, departments) {
  container.textContent = '';
  if (!Array.isArray(departments) || departments.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No persisted department rows found.';
    container.append(empty);
    return;
  }

  const list = document.createElement('ul');
  list.className = 'dept-list';

  departments.forEach((dept) => {
    const item = document.createElement('li');
    const top = document.createElement('strong');
    top.textContent = `${dept.deptName || ''} — ${dept.deptStatus || 'Not running'}`;
    item.append(top);

    const metrics = document.createElement('span');
    metrics.className = 'meta';
    metrics.textContent = `DT: ${dept.downtimeMin ?? '—'} • Eff: ${formatNumber(dept.efficiencyPct)} • Yield: ${formatNumber(dept.yieldPct)}`;
    item.append(metrics);

    const notes = document.createElement('span');
    notes.className = 'meta';
    notes.textContent = `Notes: ${dept.notes || ''}`;
    item.append(notes);

    const attachment = document.createElement('span');
    attachment.className = 'meta';
    attachment.textContent = `Attachments: ${Number(dept.attachmentCount || 0)}`;
    item.append(attachment);

    const updated = document.createElement('span');
    updated.className = 'meta';
    updated.textContent = `Updated: ${dept.updatedAt || 'n/a'} by ${dept.updatedBy || 'n/a'}`;
    item.append(updated);

    list.append(item);
  });

  container.append(list);
}

function renderAttachmentSummary(container, attachmentSummary) {
  container.textContent = '';

  if (!Array.isArray(attachmentSummary) || attachmentSummary.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No persisted attachment metadata found for this session.';
    container.append(empty);
    return;
  }

  const list = document.createElement('ul');
  list.className = 'dept-list';
  attachmentSummary.forEach((group) => {
    const item = document.createElement('li');

    const title = document.createElement('strong');
    title.textContent = `${group.deptName || ''} (${Number(group.attachmentCount || 0)})`;
    item.append(title);

    const files = document.createElement('span');
    files.className = 'meta';
    const names = Array.isArray(group.attachments)
      ? group.attachments.map((entry) => `#${entry.sequenceNo || 0} ${entry.displayName || ''}`)
      : [];
    files.textContent = names.length > 0 ? names.join(' • ') : 'No attachment file names.';
    item.append(files);

    list.append(item);
  });

  container.append(list);
}

function renderBudget(container, budgetSummary, budgetRows) {
  container.textContent = '';
  container.append(makeMeta('Planned total', formatNumber(budgetSummary?.plannedTotal)));
  container.append(makeMeta('Used total', formatNumber(budgetSummary?.usedTotal)));
  container.append(makeMeta('Variance total', formatNumber(budgetSummary?.varianceTotal)));
  container.append(makeMeta('Status', budgetSummary?.status || 'not set'));
  container.append(makeMeta('Last updated', `${budgetSummary?.lastUpdatedAt || 'n/a'} by ${budgetSummary?.lastUpdatedBy || 'n/a'}`));

  const title = document.createElement('h4');
  title.textContent = 'Budget rows';
  container.append(title);

  if (!Array.isArray(budgetRows) || budgetRows.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No persisted budget rows found.';
    container.append(empty);
    return;
  }

  const table = document.createElement('table');
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  ['Dept', 'Planned', 'Used', 'Variance', 'Status', 'Reason'].forEach((label) => {
    const th = document.createElement('th');
    th.textContent = label;
    headerRow.append(th);
  });
  thead.append(headerRow);
  table.append(thead);

  const tbody = document.createElement('tbody');
  budgetRows.forEach((row) => {
    const tr = document.createElement('tr');

    const cells = [
      row.deptName || '',
      formatNumber(row.plannedQty),
      formatNumber(row.usedQty),
      formatNumber(row.variance),
      row.status || 'not set',
      row.reasonText || ''
    ];

    cells.forEach((value) => {
      const td = document.createElement('td');
      td.textContent = value;
      tr.append(td);
    });

    tbody.append(tr);
  });

  table.append(tbody);
  container.append(table);
}

function renderReportResults(container, generatedReports) {
  container.textContent = '';

  if (!Array.isArray(generatedReports) || generatedReports.length === 0) {
    const empty = document.createElement('p');
    empty.className = 'meta';
    empty.textContent = 'No reports generated yet in this session context.';
    container.append(empty);
    return;
  }

  const list = document.createElement('ul');
  list.className = 'dept-list';
  generatedReports.forEach((result) => {
    const item = document.createElement('li');
    const head = document.createElement('strong');
    head.textContent = `${result.reportType || 'report'} • ${result.generatedAt || ''}`;
    item.append(head);

    const by = document.createElement('span');
    by.className = 'meta';
    by.textContent = `Generated by: ${result.generatedBy || 'n/a'}`;
    item.append(by);

    (Array.isArray(result.filePaths) ? result.filePaths : []).forEach((path) => {
      const pathLine = document.createElement('span');
      pathLine.className = 'meta';
      pathLine.textContent = path;
      item.append(pathLine);
    });

    list.append(item);
  });

  container.append(list);
}

export function renderPreviewScreen(root, state) {
  const sessionId = state?.session?.sessionId;

  if (!sessionId) {
    root.textContent = '';
    const panel = document.createElement('section');
    panel.className = 'panel';
    const h2 = document.createElement('h2');
    h2.textContent = 'Preview';
    panel.append(h2);
    panel.append(makeMeta('State', 'No active session loaded. Open a shift first.'));
    root.append(panel);
    return;
  }

  root.textContent = '';

  const panel = document.createElement('section');
  panel.className = 'panel';

  const h2 = document.createElement('h2');
  h2.textContent = 'Preview (read-only persisted state)';
  panel.append(h2);

  const readonly = document.createElement('p');
  readonly.className = 'meta';
  readonly.textContent = 'This screen loads saved state from host persistence only. Unsaved UI edits are not shown here.';
  panel.append(readonly);

  const message = document.createElement('p');
  message.className = 'meta';
  panel.append(message);

  const actions = document.createElement('div');
  actions.className = 'actions-row';

  const handoverButton = document.createElement('button');
  handoverButton.type = 'button';
  handoverButton.textContent = 'Generate Handover Report';

  const budgetButton = document.createElement('button');
  budgetButton.type = 'button';
  budgetButton.textContent = 'Generate Budget Report';

  const bothButton = document.createElement('button');
  bothButton.type = 'button';
  bothButton.textContent = 'Generate Both Reports';

  const openFolderButton = document.createElement('button');
  openFolderButton.type = 'button';
  openFolderButton.className = 'secondary';
  openFolderButton.textContent = 'Open Reports Folder';

  const sendButton = document.createElement('button');
  sendButton.type = 'button';
  sendButton.textContent = 'Go to Send';

  const backButton = document.createElement('button');
  backButton.type = 'button';
  backButton.className = 'secondary';
  backButton.textContent = 'Back to Dashboard';

  actions.append(handoverButton, budgetButton, bothButton, openFolderButton, sendButton, backButton);
  panel.append(actions);

  const sessionSection = makeSection('Session header');
  const deptSection = makeSection('Department summaries');
  const attachmentSection = makeSection('Attachment summary');
  const budgetSection = makeSection('Budget summary');
  const reportSection = makeSection('Generated report outputs');

  panel.append(sessionSection, deptSection, attachmentSection, budgetSection, reportSection);
  root.append(panel);

  const setBusy = (busy) => {
    handoverButton.disabled = busy;
    budgetButton.disabled = busy;
    bothButton.disabled = busy;
    openFolderButton.disabled = busy;
  };

  const refreshPreview = async () => {
    message.textContent = 'Loading persisted preview...';
    try {
      const preview = await previewService.loadPreview(sessionId);
      applyPreviewPayload(preview);
      renderSessionHeader(sessionSection, preview.session || {});
      renderDepartments(deptSection, preview.departments || []);
      renderAttachmentSummary(attachmentSection, preview.attachmentSummary || []);
      renderBudget(budgetSection, preview.budgetSummary || {}, preview.budgetRows || []);
      renderReportResults(reportSection, state.generatedReports || []);
      message.textContent = 'Preview loaded from saved state.';
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to load preview payload.';
    }
  };

  const runReport = async (runner, successLabel) => {
    setBusy(true);
    message.textContent = `${successLabel} in progress...`;
    try {
      const result = await runner();
      appendGeneratedReport(result);
      renderReportResults(reportSection, state.generatedReports || []);
      message.textContent = `${successLabel} complete.`;
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : `${successLabel} failed.`;
    } finally {
      setBusy(false);
    }
  };

  handoverButton.addEventListener('click', () => runReport(
    () => reportsService.generateHandoverReport(sessionId, state.session?.userName || ''),
    'Handover report generation'
  ));

  budgetButton.addEventListener('click', () => runReport(
    () => reportsService.generateBudgetReport(sessionId, state.session?.userName || ''),
    'Budget report generation'
  ));

  bothButton.addEventListener('click', () => runReport(
    () => reportsService.generateAllReports(sessionId, state.session?.userName || ''),
    'Combined report generation'
  ));

  openFolderButton.addEventListener('click', async () => {
    setBusy(true);
    message.textContent = 'Opening reports folder...';
    try {
      const result = await reportsService.openReportsFolder(sessionId);
      message.textContent = `Opened reports folder: ${result.openedPath || 'n/a'}`;
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to open reports folder.';
    } finally {
      setBusy(false);
    }
  });

  sendButton.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'send' } }));
  });

  backButton.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  refreshPreview();
}
