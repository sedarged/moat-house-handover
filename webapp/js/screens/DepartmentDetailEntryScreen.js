function createElement(tagName, className, text) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (text !== undefined && text !== null) element.textContent = String(text);
  return element;
}

function createNavButton(label, route, className = 'btn btn-secondary') {
  const button = createElement('button', className, label);
  button.dataset.nav = route;
  return button;
}

function addNavigateHandler(root) {
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
  }));
}

export function renderDepartmentDetailEntryScreen(root, state) {
  const shift = state.selectedShift || 'AM';
  const date = state.activeSessionDate || new Date().toLocaleDateString();
  const dept = state.activeDepartmentName || state.activeDepartment?.deptName || 'Department';
  const status = state.activeDepartment?.deptStatus || 'Not updated';

  const section = createElement('section', 'department-detail-entry');
  section.append(createElement('h2', null, 'Department Detail Entry'));

  const context = createElement('p');
  const strong = createElement('strong', null, dept);
  context.append(strong, document.createTextNode(` · ${shift} Shift · ${date}`));
  section.append(context);

  section.append(createElement('p', null, `Status: ${status}`));

  const actions = createElement('div', 'department-detail-actions');
  actions.append(
    createNavButton('Open legacy department workflow', 'department', 'btn btn-primary'),
    Object.assign(createElement('button', 'btn btn-secondary', 'Continue department notes (next phase)'), { disabled: true }),
    createNavButton('Back to Department Status Board', 'departmentBoard')
  );
  section.append(actions);
  section.append(createElement('p', 'module-screen-note', 'Full Department Detail editor comes next.'));

  root.replaceChildren(section);
  addNavigateHandler(root);
}
