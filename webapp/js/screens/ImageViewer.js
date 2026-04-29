import { attachmentsService } from '../services/attachmentsService.js';
import { applyViewerPayload, setSelectedAttachmentId } from '../state/appState.js';

function renderViewerMeta(container, current) {
  container.textContent = '';

  const name = document.createElement('p');
  const strong = document.createElement('strong');
  strong.textContent = current.displayName;
  name.append(strong);

  const dept = document.createElement('p');
  dept.className = 'meta';
  dept.textContent = `Department: ${current.deptName}`;

  const captured = document.createElement('p');
  captured.className = 'meta';
  captured.textContent = `Captured: ${current.capturedOn || 'n/a'}`;

  const path = document.createElement('p');
  path.className = 'meta';
  path.textContent = `Stored path: ${current.filePath}`;

  container.append(name, dept, captured, path);
}

function toImageUrl(attachment) {
  // Prefer the virtual URL provided by the host bridge for cross-origin file access in WebView2.
  // WebView2 (SDK 1.0.1343+) blocks cross-directory file:// requests; the host maps
  // moat-attachments.local to the attachments root via SetVirtualHostNameToFolderMapping.
  if (attachment.virtualUrl) {
    return attachment.virtualUrl;
  }
  return `file:///${String(attachment.filePath).replaceAll('\\', '/')}`;
}

export function renderImageViewerScreen(root, state) {
  const sessionId = state.session?.sessionId;
  const deptRecordId = state.activeDepartment?.deptRecordId;
  const selectedAttachmentId = state.selectedAttachmentId;

  if (!sessionId || !deptRecordId || !selectedAttachmentId) {
    root.innerHTML = `
      <section class="panel">
        <h2>Image Viewer</h2>
        <p class="meta">No attachment selected.</p>
        <button id="viewer-back" type="button" class="secondary">Back to Department</button>
      </section>
    `;
    root.querySelector('#viewer-back')?.addEventListener('click', () => {
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department' } }));
    });
    return;
  }

  root.innerHTML = `
    <section class="panel">
      <h2>Image Viewer</h2>
      <p id="viewer-message" class="meta">Loading selected attachment...</p>
      <div id="viewer-meta"></div>
      <div class="viewer-image-wrap">
        <img id="viewer-image" class="viewer-image" alt="Department attachment" />
      </div>
      <div class="actions-row">
        <button id="viewer-prev" type="button" class="secondary">Previous</button>
        <button id="viewer-next" type="button" class="secondary">Next</button>
        <button id="viewer-close" type="button">Back to Department</button>
      </div>
    </section>
  `;

  const viewerMessage = root.querySelector('#viewer-message');
  const viewerMeta = root.querySelector('#viewer-meta');
  const viewerImage = root.querySelector('#viewer-image');
  const prevButton = root.querySelector('#viewer-prev');
  const nextButton = root.querySelector('#viewer-next');

  async function loadAttachment(attachmentId) {
    viewerMessage.textContent = 'Loading viewer payload...';
    try {
      const payload = await attachmentsService.openViewer(sessionId, deptRecordId, attachmentId);
      applyViewerPayload(payload);

      const current = payload.current;
      viewerMessage.textContent = `Attachment ${payload.currentIndex + 1} of ${payload.totalCount}`;
      renderViewerMeta(viewerMeta, current);

      viewerImage.src = toImageUrl(current);
      viewerImage.dataset.attachmentId = String(current.attachmentId);

      prevButton.disabled = !payload.previous;
      nextButton.disabled = !payload.next;

      prevButton.onclick = async () => {
        if (!payload.previous) {
          return;
        }

        setSelectedAttachmentId(payload.previous.attachmentId);
        await loadAttachment(payload.previous.attachmentId);
      };

      nextButton.onclick = async () => {
        if (!payload.next) {
          return;
        }

        setSelectedAttachmentId(payload.next.attachmentId);
        await loadAttachment(payload.next.attachmentId);
      };
    } catch (error) {
      viewerMessage.textContent = error instanceof Error ? error.message : 'Failed to load viewer.';
      viewerMeta.textContent = '';
      viewerImage.removeAttribute('src');
      prevButton.disabled = true;
      nextButton.disabled = true;
    }
  }

  root.querySelector('#viewer-close')?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department' } }));
  });

  void loadAttachment(selectedAttachmentId);
}
