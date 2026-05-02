export function renderDepartmentDetailEntryScreen(root, state) {
  const shift = state.selectedShift || 'AM';
  const date = state.activeSessionDate || new Date().toLocaleDateString();
  const dept = state.activeDepartmentName || 'Department';
  root.innerHTML = `<section class="department-detail-entry">
    <h2>Department Detail Entry</h2>
    <p><strong>${dept}</strong> · ${shift} Shift · ${date}</p>
    <p>Status: ${state.activeDepartment?.deptStatus || 'Not updated'}</p>
    <div class="department-detail-actions">
      <button class="btn btn-primary" data-nav="department">Open legacy department workflow</button>
      <button class="btn btn-secondary" disabled>Continue department notes (next phase)</button>
      <button class="btn btn-secondary" data-nav="departmentBoard">Back to Department Status Board</button>
    </div>
    <p class="module-screen-note">Full Department Detail editor comes next.</p>
  </section>`;
  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }))));
}
