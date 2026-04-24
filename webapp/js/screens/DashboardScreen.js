import { sessionService } from '../services/sessionService.js';
import { applySessionPayload } from '../state/appState.js';

function renderDepartments(departments) {
  if (!departments?.length) {
    return '<li>No department rows loaded.</li>';
  }

  return departments
    .map((dept) => `<li><strong>${dept.deptName}</strong> — ${dept.deptStatus}</li>`)
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

  root.innerHTML = `
    <section class="panel">
      <h2>Dashboard</h2>
      <p class="meta">Session #${session.sessionId} • ${session.shiftCode} • ${session.shiftDate} • ${session.sessionStatus}</p>
      <p class="meta">Updated: ${session.updatedAt || 'n/a'} by ${session.updatedBy || 'n/a'}</p>

      <h3>Department Summary</h3>
      <ul class="dept-list">${renderDepartments(session.departments)}</ul>

      <div class="actions-row">
        <button id="clear-day-btn" type="button">Clear Day</button>
        <button id="back-shift-btn" type="button" class="secondary">Back to Shift</button>
      </div>
      <p id="dashboard-message" class="meta"></p>
    </section>
  `;

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
