import { placeholderTemplate } from '../core/template.js';

export function renderPreviewScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Preview Screen',
    'Read-only consolidated handover preview before report generation and send.',
    ['Generate handover report', 'Generate budget report', 'Open output folder', 'Go to send screen']
  );
}
