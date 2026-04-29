import { attachmentsService } from '../services/attachmentsService.js';
import { applyViewerPayload, setSelectedAttachmentId } from '../state/appState.js';
import {
  iconBrandSvg, iconArrowLeft, iconBell, iconCalendar, iconClockSm,
  iconPrev, iconNext
} from '../core/icons.js';

function formatDate(iso) {
  if (!iso) return '—';
  try { const [y, m, d] = iso.split('-'); return `${d}/${m}/${y}`; } catch { return iso; }
}

function toFileUrl(path) {
  return `file:///${String(path).replaceAll('\\', '/')}`;
}

export function renderImageViewerScreen(root, state) {
  const sessionId          = state.session?.sessionId;
  const deptRecordId       = state.activeDepartment?.deptRecordId;
  const selectedAttachment = state.selectedAttachmentId;

  root.innerHTML = '';
  const screen = document.createElement('div');
  screen.className = 'screen';

  if (!sessionId || !deptRecordId || !selectedAttachment) {
    screen.innerHTML = `
      <header class="screen-header">
        <button class="header-back" id="viewer-back" type="button">${iconArrowLeft}</button>
        <div class="header-brand">${iconBrandSvg}<div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div></div>
        <div class="header-title">IMAGE VIEWER</div>
      </header>
      <div class="screen-content" style="display:flex;align-items:center;justify-content:center;">
        <div style="text-align:center;">
          <p class="status-line" style="margin-bottom:1rem;">No attachment selected.</p>
          <button class="btn" id="viewer-back-btn" type="button">${iconArrowLeft}&nbsp; Back to Department</button>
        </div>
      </div>
    `;
    root.append(screen);
    const goBack = () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department' } }));
    screen.querySelector('#viewer-back')?.addEventListener('click', goBack);
    screen.querySelector('#viewer-back-btn')?.addEventListener('click', goBack);
    return;
  }

  screen.innerHTML = `
    <header class="screen-header">
      <button class="header-back" id="viewer-back" type="button">${iconArrowLeft}</button>
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">IMAGE VIEWER</div>
      <div class="header-actions">
        <button class="header-icon-btn" type="button">${iconBell}</button>
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
      <div class="infobar-item" id="viewer-counter">— of —</div>
    </div>

    <div class="screen-content" style="display:flex;flex-direction:column;gap:0.75rem;">
      <p class="status-line" id="viewer-message">Loading attachment…</p>

      <div style="display:grid;grid-template-columns:1fr 260px;gap:1rem;align-items:start;flex:1;">
        <!-- image -->
        <div>
          <div class="viewer-image-wrap">
            <img id="viewer-image" class="viewer-image" alt="Department attachment" style="display:none;" />
            <p id="viewer-placeholder" style="color:var(--muted);font-size:0.9rem;">Image loading…</p>
          </div>
        </div>

        <!-- meta -->
        <div class="section-block" style="margin-bottom:0;">
          <div class="section-header">
            <span class="section-title">Attachment Details</span>
          </div>
          <div id="viewer-meta"></div>
        </div>
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost" id="viewer-close"  type="button">${iconArrowLeft}&nbsp; Department</button>
      <button class="btn" id="viewer-prev"             type="button" disabled>${iconPrev}&nbsp; Previous</button>
      <button class="btn" id="viewer-next"             type="button" disabled>Next&nbsp;${iconNext}</button>
    </footer>
  `;

  root.append(screen);

  const viewerMsg    = screen.querySelector('#viewer-message');
  const viewerImage  = screen.querySelector('#viewer-image');
  const placeholder  = screen.querySelector('#viewer-placeholder');
  const viewerMeta   = screen.querySelector('#viewer-meta');
  const viewerCounter= screen.querySelector('#viewer-counter');
  const prevBtn      = screen.querySelector('#viewer-prev');
  const nextBtn      = screen.querySelector('#viewer-next');

  const goBack = () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department' } }));
  screen.querySelector('#viewer-back')?.addEventListener('click', goBack);
  screen.querySelector('#viewer-close')?.addEventListener('click', goBack);

  function renderMeta(current) {
    viewerMeta.innerHTML = `
      <div class="field-row"><span class="field-label">Name</span><span class="field-value"><strong>${current.displayName}</strong></span></div>
      <div class="field-row"><span class="field-label">Department</span><span class="field-value">${current.deptName || '—'}</span></div>
      <div class="field-row"><span class="field-label">Captured</span><span class="field-value">${current.capturedOn || 'n/a'}</span></div>
      <div class="field-row"><span class="field-label">File</span><span class="field-value" style="word-break:break-all;font-size:0.75rem;color:var(--muted);">${current.filePath || '—'}</span></div>
    `;
  }

  async function loadAttachment(attachmentId) {
    viewerMsg.textContent = 'Loading…';
    viewerMsg.className   = 'status-line';
    viewerImage.style.display = 'none';
    placeholder.style.display = '';
    placeholder.textContent   = 'Loading image…';

    try {
      const payload = await attachmentsService.openViewer(sessionId, deptRecordId, attachmentId);
      applyViewerPayload(payload);

      const current = payload.current;
      viewerMsg.textContent   = '';
      viewerCounter.textContent = `${payload.currentIndex + 1} of ${payload.totalCount}`;

      renderMeta(current);

      /* load image */
      viewerImage.onload = () => {
        placeholder.style.display  = 'none';
        viewerImage.style.display  = '';
      };
      viewerImage.onerror = () => {
        placeholder.textContent = 'Cannot preview this file type or the file was not found.';
        placeholder.style.display = '';
        viewerImage.style.display = 'none';
      };
      viewerImage.src = toFileUrl(current.filePath);
      viewerImage.dataset.attachmentId = String(current.attachmentId);

      prevBtn.disabled = !payload.previous;
      nextBtn.disabled = !payload.next;

      prevBtn.onclick = payload.previous ? async () => {
        setSelectedAttachmentId(payload.previous.attachmentId);
        await loadAttachment(payload.previous.attachmentId);
      } : null;

      nextBtn.onclick = payload.next ? async () => {
        setSelectedAttachmentId(payload.next.attachmentId);
        await loadAttachment(payload.next.attachmentId);
      } : null;

    } catch (e) {
      viewerMsg.textContent   = e instanceof Error ? e.message : 'Failed to load viewer.';
      viewerMsg.className     = 'status-line error';
      viewerMeta.innerHTML    = '';
      placeholder.textContent = 'Could not load attachment.';
      viewerImage.style.display = 'none';
      prevBtn.disabled = true;
      nextBtn.disabled = true;
    }
  }

  void loadAttachment(selectedAttachment);
}
