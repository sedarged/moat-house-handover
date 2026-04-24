import { placeholderTemplate } from '../core/template.js';

export function renderBudgetScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Budget Screen',
    'Edit planned/used/variance rows and save budget summary for the session.',
    ['Save budget', 'Recalculate totals', 'Return to dashboard']
  );
}
