import { sessionService } from '../services/sessionService.js';
import { applySessionPayload } from '../state/appState.js';

function todayIsoDate() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = `${now.getMonth() + 1}`.padStart(2, '0');
  const dd = `${now.getDate()}`.padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

export function renderShiftScreen(root, state) {
  root.innerHTML = `
    <section class="panel">
      <h2>Shift Session</h2>
      <p class="meta">Open existing AM/PM/NS session by date, or create a blank day when none exists.</p>
      <form id="shift-form" class="form-grid">
        <label>
          Shift
          <select name="shiftCode" required>
            <option value="AM">AM</option>
            <option value="PM">PM</option>
            <option value="NS">NS</option>
          </select>
        </label>
        <label>
          Shift Date
          <input type="date" name="shiftDate" required value="${state.session.shiftDate || todayIsoDate()}" />
        </label>
        <label>
          User
          <input type="text" name="userName" value="${state.session.userName || ''}" placeholder="Current user" />
        </label>
        <div class="actions-row">
          <button type="submit">Open Session</button>
        </div>
      </form>
      <p id="shift-message" class="meta"></p>
    </section>
  `;

  const form = root.querySelector('#shift-form');
  const message = root.querySelector('#shift-message');

  form?.addEventListener('submit', async (event) => {
    event.preventDefault();
    message.textContent = 'Checking existing session...';

    const formData = new FormData(form);
    const shiftCode = String(formData.get('shiftCode') || '');
    const shiftDate = String(formData.get('shiftDate') || '');
    const userName = String(formData.get('userName') || '');

    try {
      const openResult = await sessionService.openSession(shiftCode, shiftDate, userName);
      if (openResult.found && openResult.session) {
        applySessionPayload(openResult.session);
        message.textContent = `Loaded existing session #${openResult.session.sessionId}.`;
        window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
        return;
      }

      const createConfirmed = window.confirm(`No ${shiftCode} session exists for ${shiftDate}. Create a blank session now?`);
      if (!createConfirmed) {
        message.textContent = 'No session opened.';
        return;
      }

      message.textContent = 'Creating blank session...';
      const createResult = await sessionService.createBlankSession(shiftCode, shiftDate, userName);
      applySessionPayload(createResult.session);
      message.textContent = `Created blank session #${createResult.session.sessionId}.`;
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
    } catch (error) {
      message.textContent = error instanceof Error ? error.message : 'Unexpected session error.';
    }
  });
}
