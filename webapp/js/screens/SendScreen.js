import { placeholderTemplate } from '../core/template.js';

export function renderSendScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Send Screen',
    'Validate output and create local email draft using shift-based profile.',
    ['Re-run validation', 'Create draft', 'Open generated files']
  );
}
