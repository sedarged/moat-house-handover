import { placeholderTemplate } from '../core/template.js';

export function renderDashboardScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Dashboard Screen',
    'Operational overview with department tiles, closure summary, and budget summary.',
    ['Open department', 'Open budget', 'Open preview', 'Clear day', 'Generate reports']
  );
}
