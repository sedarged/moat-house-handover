import { sendService } from '../services/sendService.js';
import { applySendPackagePayload } from '../state/appState.js';

function makeField(label, value) {
  const wrap = document.createElement('div');
  wrap.className = 'meta';

  const strong = document.createElement('strong');
  strong.textContent = `${label}: `;
  wrap.append(strong);

  const span = document.createElement('span');
  span.textContent = value || '—';
  wrap.append(span);

  return wrap;
}

function renderLines(container, values, emptyText) {
  container.textContent = '';
  if (!Array.isArray(values) || values.length === 0) {
    const line = document.createElement('li');
    line.textContent = emptyText;
    container.append(line);
    return;
  }

  values.forEach((value) => {
    const line = document.createElement('li');
    line.textContent = value;
    container.append(line);
  });
}

export function renderSendScreen(root, state) {
  const sessionId = state?.session?.sessionId;

  root.textContent = '';
  const panel = document.createElement('section');
  panel.className = 'panel';

  const title = document.createElement('h2');
  title.textContent = 'Send Workflow (package + Outlook draft only)';
  panel.append(title);

  const hint = document.createElement('p');
  hint.className = 'meta';
  hint.textContent = 'This screen prepares an email package and creates an Outlook draft only. No automatic send is performed.';
  panel.append(hint);

  const message = document.createElement('p');
  message.className = 'meta';
  panel.append(message);

  const actions = document.createElement('div');
  actions.className = 'actions-row';

  const prepareButton = document.createElement('button');
  prepareButton.type = 'button';
  prepareButton.textContent = 'Prepare Package';

  const draftButton = document.createElement('button');
  draftButton.type = 'button';
  draftButton.textContent = 'Create Outlook Draft';

  const backPreviewButton = document.createElement('button');
  backPreviewButton.type = 'button';
  backPreviewButton.className = 'secondary';
  backPreviewButton.textContent = 'Back to Preview';

  const backDashboardButton = document.createElement('button');
  backDashboardButton.type = 'button';
  backDashboardButton.className = 'secondary';
  backDashboardButton.textContent = 'Back to Dashboard';

  actions.append(prepareButton, draftButton, backPreviewButton, backDashboardButton);
  panel.append(actions);

  const overview = document.createElement('div');
  panel.append(overview);

  const subjectLabel = document.createElement('h3');
  subjectLabel.textContent = 'Subject';
  panel.append(subjectLabel);
  const subjectBox = document.createElement('textarea');
  subjectBox.readOnly = true;
  subjectBox.rows = 2;
  panel.append(subjectBox);

  const bodyLabel = document.createElement('h3');
  bodyLabel.textContent = 'Body Preview';
  panel.append(bodyLabel);
  const bodyBox = document.createElement('textarea');
  bodyBox.readOnly = true;
  bodyBox.rows = 8;
  panel.append(bodyBox);

  const attachmentsTitle = document.createElement('h3');
  attachmentsTitle.textContent = 'Report Attachments';
  panel.append(attachmentsTitle);
  const attachmentList = document.createElement('ul');
  attachmentList.className = 'dept-list';
  panel.append(attachmentList);

  const validationTitle = document.createElement('h3');
  validationTitle.textContent = 'Validation Messages';
  panel.append(validationTitle);
  const validationList = document.createElement('ul');
  validationList.className = 'dept-list';
  panel.append(validationList);

  root.append(panel);

  const setBusy = (busy) => {
    prepareButton.disabled = busy;
    draftButton.disabled = busy;
  };

  const renderPackage = (pkg) => {
    overview.textContent = '';

    overview.append(makeField('Session', String(pkg?.sessionId || sessionId || '—')));
    overview.append(makeField('Shift', `${pkg?.shiftCode || state?.session?.shiftCode || '—'} • ${pkg?.shiftDate || state?.session?.shiftDate || '—'}`));
    overview.append(makeField('Email profile', pkg?.emailProfileKey || '—'));
    overview.append(makeField('To', pkg?.toList || '—'));
    overview.append(makeField('CC', pkg?.ccList || '—'));
    overview.append(makeField('Readiness', `${pkg?.readinessStatus || 'not prepared'} • ready=${pkg?.isReady ? 'yes' : 'no'}`));
    overview.append(makeField('Generated', `${pkg?.generatedAt || '—'} by ${pkg?.generatedBy || '—'}`));

    subjectBox.value = pkg?.subject || '';
    bodyBox.value = pkg?.body || '';

    renderLines(attachmentList, pkg?.attachmentPaths || [], 'No report attachments prepared.');
    renderLines(validationList, pkg?.validationMessages || [], 'No validation messages. Package is ready.');
  };

  const loadPrepared = () => {
    renderPackage(state.sendPackage || null);
  };

  prepareButton.addEventListener('click', async () => {
    if (!sessionId) {
      message.textContent = 'No active session loaded. Open a shift first.';
      return;
    }

    setBusy(true);
    message.textContent = 'Preparing send package...';
    try {
      const result = await sendService.preparePackage(sessionId, state.session?.userName || '');
      applySendPackagePayload(result.package);
      renderPackage(result.package);
      message.textContent = result.success
        ? 'Send package prepared and validated.'
        : 'Send package prepared with validation errors. Review messages before creating draft.';
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to prepare send package.';
    } finally {
      setBusy(false);
    }
  });

  draftButton.addEventListener('click', async () => {
    if (!sessionId) {
      message.textContent = 'No active session loaded. Open a shift first.';
      return;
    }

    setBusy(true);
    message.textContent = 'Creating Outlook draft...';
    try {
      const result = await sendService.createOutlookDraft(sessionId, state.session?.userName || '');
      applySendPackagePayload(result.package);
      renderPackage(result.package);
      if (result.success) {
        message.textContent = result.draft?.message || 'Outlook draft created. Email was not sent.';
      } else {
        message.textContent = result.draft?.message || 'Outlook draft was not created. Review validation/errors and retry.';
      }
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Failed to create Outlook draft.';
    } finally {
      setBusy(false);
    }
  });

  backPreviewButton.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'preview' } }));
  });

  backDashboardButton.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  });

  loadPrepared();
}
