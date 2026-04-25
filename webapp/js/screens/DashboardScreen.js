import { sessionService } from '../services/sessionService.js';
import { applySessionPayload, setActiveDepartmentName } from '../state/appState.js';

function createDepartmentList(container, departments) {
  container.textContent = '';

  if (!departments?.length) {
    const empty = document.createElement('li');
    empty.textContent = 'No department rows loaded.';
    container.append(empty);
    return;
  }

  departments.forEach((dept) => {
    const item = document.createElement('li');

    const title = document.createElement('strong');
    title.textContent = dept.deptName || '';
    item.append(title);
    item.append(` — ${dept.deptStatus || 'Not running'}`);

    const attachmentMeta = document.createElement('span');
    attachmentMeta.className = 'meta';
    attachmentMeta.textContent = `Attachments: ${Number(dept.attachmentCount || 0)}`;
    item.append(attachmentMeta);

    const updatedMeta = document.createElement('span');
    updatedMeta.className = 'meta';
    updatedMeta.textContent = `Updated: ${dept.updatedAt || 'n/a'} by ${dept.updatedBy || 'n/a'}`;
    item.append(updatedMeta);

    const openButton = document.createElement('button');
    openButton.className = 'secondary';
    openButton.type = 'button';
    openButton.textContent = 'Open';
    openButton.addEventListener('click', () => {
      const deptName = dept.deptName || null;
      setActiveDepartmentName(deptName);
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'department', deptName } }));
    });
    item.append(openButton);

    container.append(item);
  });
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
      <p id="dashboard-session" class="meta"></p>
      <p id="dashboard-updated" class="meta"></p>
      <p id="dashboard-total-attachments" class="meta"></p>

      <h3>Department Summary</h3>
      <ul id="dashboard-dept-list" class="dept-list"></ul>

      <div class="actions-row">
        <button id="clear-day-btn" type="button">Clear Day</button>
        <button id="back-shift-btn" type="button" class="secondary">Back to Shift</button>
      </div>
      <p id="dashboard-message" class="meta"></p>
    </section>
  `;

  const sessionText = root.querySelector('#dashboard-session');
  const updatedText = root.querySelector('#dashboard-updated');
  const totalText = root.querySelector('#dashboard-total-attachments');
  const deptList = root.querySelector('#dashboard-dept-list');
  const msg = root.querySelector('#dashboard-message');
  const clearButton = root.querySelector('#clear-day-btn');
  const backButton = root.querySelector('#back-shift-btn');

  const totalAttachments = (session.departments || []).reduce((sum, dept) => sum + Number(dept.attachmentCount || 0), 0);

  sessionText.textContent = `Session #${session.sessionId} • ${session.shiftCode} • ${session.shiftDate} • ${session.sessionStatus}`;
  updatedText.textContent = `Updated: ${session.updatedAt || 'n/a'} by ${session.updatedBy || 'n/a'}`;
  totalText.textContent = `Total attachments in session: ${totalAttachments}`;

  createDepartmentList(deptList, session.departments || []);

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
