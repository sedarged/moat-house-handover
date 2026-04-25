import { sessionService } from '../services/sessionService.js';
import { applySessionPayload, setActiveDepartmentName } from '../state/appState.js';

function escapeHtml(value) {
  return String(value ?? '')
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function renderDepartments(departments) {
  if (!departments?.length) {
    return '<li>No department rows loaded.</li>';
  }

  return departments
    .map((dept) => {
      const safeDeptName = escapeHtml(dept.deptName);
      const safeDeptStatus = escapeHtml(dept.deptStatus);
      const safeUpdatedAt = escapeHtml(dept.updatedAt || 'n/a');
      const safeUpdatedBy = escapeHtml(dept.updatedBy || 'n/a');
      const safeAttachmentCount = escapeHtml(dept.attachmentCount ?? 0);

      return `
      <li>
        <strong>${safeDeptName}</strong> — ${safeDeptStatus}
        <span class="meta">Attachments: ${safeAttachmentCount}</span>
        <span class="meta">Updated: ${safeUpdatedAt} by ${safeUpdatedBy}</span>
        <button class="secondary" type="button" data-open-dept="${safeDeptName}">Open</button>
      </li>`;
    })
    .join('');
}

export function renderDashboardScreen(root, state) {
  const { session } = state;

  if (!session?.sessionId) {
    root.innerHTML = `
      <section class="panel">
        <h2>Dashboard</h2>
        <p class="meta">No active session loaded. Open a shift session first.</p>
      </section>
    `;
    return;
  }

  const safeSessionId = escapeHtml(session.sessionId);
  const safeShiftCode = escapeHtml(session.shiftCode);
  const safeShiftDate = escapeHtml(session.shiftDate);
  const safeSessionStatus = escapeHtml(session.sessionStatus);
  const safeUpdatedAt = escapeHtml(session.updatedAt || 'n/a');
  const safeUpdatedBy = escapeHtml(session.updatedBy || 'n/a');
  const totalAttachments = (session.departments || []).reduce((sum, dept) => sum + Number(dept.attachmentCount || 0), 0);

  root.innerHTML = `
    <section class="panel">
      <h2>Dashboard</h2>
      <p class="meta">Session #${safeSessionId} • ${safeShiftCode} • ${safeShiftDate} • ${safeSessionStatus}</p>
      <p class="meta">Updated: ${safeUpdatedAt} by ${safeUpdatedBy}</p>
      <p class="meta">Total attachments in session: ${escapeHtml(totalAttachments)}</p>

      <h3>Department Summary</h3>
      <ul class="dept-list">${renderDepartments(session.departments)}</ul>

      <div class="actions-row">
        <button id="clear-day-btn" type="button">Clear Day</button>
        <button id="back-shift-btn" type="button" class="secondary">Back to Shift</button>
      </div>
      <p id="dashboard-message" class="meta"></p>
    </section>
  `;

  root.querySelectorAll('[data-open-dept]').forEach((button) => {
    button.addEventListener('click', () => {
      const deptName = button.getAttribute('data-open-dept');
      setActiveDepartmentName(deptName);
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department', deptName } }));
    });
  });

  const msg = root.querySelector('#dashboard-message');
  const clearButton = root.querySelector('#clear-day-btn');
  const backButton = root.querySelector('#back-shift-btn');

  clearButton?.addEventListener('click', async () => {
    const confirmed = window.confirm('Clear current day? This resets department, attachment, and budget rows for this session.');
    if (!confirmed) {
      return;
    }

    try {
      msg.textContent = 'Clearing day...';
      const result = await sessionService.clearDay(session.sessionId, session.userName || '');
      applySessionPayload(result.session);
      msg.textContent = 'Day cleared and reset from persisted data.';
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
    } catch (error) {
      msg.textContent = error instanceof Error ? error.message : 'Failed to clear day.';
    }
  });

  backButton?.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'shift' } }));
  });
}
