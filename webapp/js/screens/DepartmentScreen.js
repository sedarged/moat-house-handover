import { placeholderTemplate } from '../core/template.js';

export function renderDepartmentScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Department Screen',
    'Edit one department status, notes, and department attachment list.',
    ['Save department', 'Add/remove attachment', 'Prev/next attachment', 'Open full image viewer']
  );
}
