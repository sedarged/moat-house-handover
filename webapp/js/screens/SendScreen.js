import { sendService } from '../services/sendService.js';
import { applySendPackagePayload } from '../state/appState.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconUser, iconCalendar,
  iconClockSm, iconSend, iconRefresh, iconClipboard, iconChart
} from '../core/icons.js';

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function fieldRow(label, value, valueCls = '') {
  return `<div class="field-row">
    <span class="field-label">${label}</span>
    <span class="field-value ${valueCls}">${value || '—'}</span>
  </div>`;
}

export function renderSendScreen(root, state) {
  const sessionId = state?.session?.sessionId;

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="send-back-hdr" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">SEND — OUTLOOK DRAFT</div>
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
        <span class="infobar-icon">${iconSend}</span>
        <span>Draft only — no automatic send</span>
      </div>
      <div class="infobar-item" id="readiness-badge"></div>
    </div>

    <div class="screen-content">
      <p class="status-line" id="send-message" style="margin-bottom:0.75rem;"></p>

      ${!sessionId ? `<div class="section-block"><p class="status-line error">No active session. Open a shift session first.</p></div>` : ''}

      <div class="two-col" id="send-main" ${!sessionId ? 'style="display:none"' : ''}>
        <!-- LEFT column -->
        <div>
          <div class="section-block">
            <div class="section-header">
              <span class="section-icon">${iconClipboard}</span>
              <span class="section-title">Package Overview</span>
            </div>
            <div id="pkg-overview"></div>
          </div>

          <div class="section-block">
            <div class="section-header">
              <span class="section-icon">${iconChart}</span>
              <span class="section-title">Report Attachments</span>
            </div>
            <ul id="pkg-attachments" style="list-style:none;font-size:0.85rem;color:var(--muted);"></ul>
          </div>

          <div class="section-block">
            <div class="section-header">
              <span class="section-icon">${iconClipboard}</span>
              <span class="section-title">Validation Messages</span>
            </div>
            <ul id="pkg-validation" style="list-style:none;font-size:0.85rem;"></ul>
          </div>
        </div>

        <!-- RIGHT column -->
        <div>
          <div class="section-block">
            <div class="section-header">
              <span class="section-title">Email Subject</span>
            </div>
            <textarea class="send-textarea" id="send-subject" rows="2" readonly placeholder="Prepare package to populate…"></textarea>
          </div>
          <div class="section-block">
            <div class="section-header">
              <span class="section-title">Email Body Preview</span>
            </div>
            <textarea class="send-textarea" id="send-body" rows="12" readonly placeholder="Prepare package to populate…"></textarea>
          </div>
        </div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost"   id="send-back-preview"   type="button">${iconArrowLeft}&nbsp; Preview</button>
      <button class="btn btn-ghost"   id="send-back-dashboard" type="button">${iconArrowLeft}&nbsp; Dashboard</button>
      <button class="btn"             id="send-prepare"        type="button" ${!sessionId ? 'disabled' : ''}>${iconRefresh}&nbsp; Prepare Package</button>
      <button class="btn btn-primary" id="send-draft"          type="button" ${!sessionId ? 'disabled' : ''}>${iconSend}&nbsp; Create Outlook Draft</button>
    </footer>
  `;

  root.append(screen);

  if (!sessionId) {
    screen.querySelector('#send-back-hdr')?.addEventListener('click', () =>
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } })));
    return;
  }

  const msg            = screen.querySelector('#send-message');
  const pkgOverview    = screen.querySelector('#pkg-overview');
  const pkgAttachments = screen.querySelector('#pkg-attachments');
  const pkgValidation  = screen.querySelector('#pkg-validation');
  const subjectBox     = screen.querySelector('#send-subject');
  const bodyBox        = screen.querySelector('#send-body');
  const readinessBadge = screen.querySelector('#readiness-badge');
  const prepareBtn     = screen.querySelector('#send-prepare');
  const draftBtn       = screen.querySelector('#send-draft');

  const goBack = (route) => () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
  screen.querySelector('#send-back-hdr')?.addEventListener('click', goBack('dashboard'));
  screen.querySelector('#send-back-preview')?.addEventListener('click', goBack('preview'));
  screen.querySelector('#send-back-dashboard')?.addEventListener('click', goBack('dashboard'));

  const setBusy = (busy) => { prepareBtn.disabled = busy; draftBtn.disabled = busy; };

  function renderReadinessBadge(pkg) {
    if (!pkg) { readinessBadge.innerHTML = ''; return; }
    const isReady = pkg.isReady;
    const cls = isReady ? 'value-green' : 'value-orange';
    readinessBadge.innerHTML = `<span style="font-size:0.76rem;padding:0.2rem 0.55rem;border-radius:4px;border:1px solid var(--border);background:var(--surface-2);">
      Status: <strong class="${cls}">${pkg.readinessStatus || (isReady ? 'Ready' : 'Not ready')}</strong>
    </span>`;
  }

  function renderPackage(pkg) {
    if (!pkg) return;

    pkgOverview.innerHTML =
      fieldRow('Session', String(pkg.sessionId || sessionId || '—')) +
      fieldRow('Shift', `${pkg.shiftCode || state?.session?.shiftCode || '—'} · ${pkg.shiftDate || state?.session?.shiftDate || '—'}`) +
      fieldRow('Email Profile', pkg.emailProfileKey || '—') +
      fieldRow('To', pkg.toList || '—') +
      fieldRow('CC', pkg.ccList || '—') +
      fieldRow('Generated', `${pkg.generatedAt || '—'} by ${pkg.generatedBy || '—'}`);

    pkgAttachments.innerHTML = '';
    const paths = Array.isArray(pkg.attachmentPaths) ? pkg.attachmentPaths : [];
    if (!paths.length) {
      pkgAttachments.innerHTML = '<li style="color:var(--muted)">No report attachments prepared.</li>';
    } else {
      paths.forEach((p) => {
        const li = document.createElement('li');
        li.style.cssText = 'padding:0.25rem 0;border-bottom:1px solid var(--border);word-break:break-all;';
        li.textContent = p;
        pkgAttachments.append(li);
      });
    }

    pkgValidation.innerHTML = '';
    const msgs = Array.isArray(pkg.validationMessages) ? pkg.validationMessages : [];
    if (!msgs.length) {
      pkgValidation.innerHTML = '<li style="color:var(--muted-light)">No validation messages — package is ready.</li>';
    } else {
      msgs.forEach((m) => {
        const li = document.createElement('li');
        li.style.cssText = 'padding:0.25rem 0;color:#f87171;';
        li.textContent = m;
        pkgValidation.append(li);
      });
    }

    subjectBox.value = pkg.subject || '';
    bodyBox.value    = pkg.body    || '';
    renderReadinessBadge(pkg);
  }

  /* show existing package if already prepared */
  if (state.sendPackage) renderPackage(state.sendPackage);

  prepareBtn.addEventListener('click', async () => {
    setBusy(true);
    msg.textContent = 'Preparing send package…';
    msg.className   = 'status-line';
    try {
      const result = await sendService.preparePackage(sessionId, state.session?.userName || '');
      applySendPackagePayload(result.package);
      renderPackage(result.package);
      msg.textContent = result.success
        ? 'Package prepared and validated.'
        : 'Package prepared with validation warnings. Review before creating draft.';
      msg.className = result.success ? 'status-line success' : 'status-line warn';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to prepare package.';
      msg.className   = 'status-line error';
    } finally { setBusy(false); }
  });

  draftBtn.addEventListener('click', async () => {
    setBusy(true);
    msg.textContent = 'Creating Outlook draft…';
    msg.className   = 'status-line';
    try {
      const result = await sendService.createOutlookDraft(sessionId, state.session?.userName || '');
      applySendPackagePayload(result.package);
      renderPackage(result.package);
      msg.textContent = result.success
        ? result.draft?.message || 'Outlook draft created. Email was NOT sent.'
        : result.draft?.message || 'Draft not created. Review validation messages and retry.';
      msg.className = result.success ? 'status-line success' : 'status-line error';
    } catch (e) {
      msg.textContent = e instanceof Error ? e.message : 'Failed to create Outlook draft.';
      msg.className   = 'status-line error';
    } finally { setBusy(false); }
  });
}
