import { placeholderTemplate } from '../core/template.js';

export function renderShiftScreen(root) {
  root.innerHTML = placeholderTemplate(
    'Shift Screen',
    'Choose AM/PM/NS and a shift date before opening a session.',
    ['Open selected shift/date', 'Load existing session', 'Prompt to create blank session']
  );
}
