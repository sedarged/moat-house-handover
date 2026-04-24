import { placeholderTemplate } from '../core/template.js';

export function renderImageViewerScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Image Viewer',
    'Full-size attachment image viewer with metadata and navigation controls.',
    ['Previous image', 'Next image', 'Close viewer']
  );
}
