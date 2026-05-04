import { applySessionPayload, setActiveSessionContext } from '../state/appState.js';
import { sessionService } from '../services/sessionService.js';

const STATUS_OPTIONS = ['All', 'Open', 'Draft', 'Closed'];

function text(value, fallback = '—') { return value == null || value === '' ? fallback : String(value); }
function normaliseDate(value) { return String(value || '').split('T')[0]; }

function validateSessionPayload(payload) {
  const required = ['sessionId', 'shiftCode', 'shiftDate', 'sessionStatus', 'departments'];
  const missing = required.filter((key) => payload?.[key] == null);
  if (missing.length > 0) throw new Error(`Session payload missing required fields: ${missing.join(', ')}`);
}

function mapShiftLabel(code) { return code === 'NS' ? 'Night Shift' : `${code} Shift`; }

export function renderExistingHandoverPickerScreen(root, state) {
  const selectedShift = state.selectedShift || 'AM';
  root.innerHTML = `<section class="existing-handover-picker">
    <div class="session-header-card">
      <p class="session-kicker">${mapShiftLabel(selectedShift)}</p>
      <h2>Open Existing Handover</h2>
      <p>Select a saved handover session and load it into the active workflow.</p>
    </div>
    <div class="picker-filters">
      <label>Shift<select data-filter="shift"><option value="">All</option><option value="AM">AM</option><option value="PM">PM</option><option value="NS">NS</option></select></label>
      <label>Date<input data-filter="date" type="date" /></label>
      <label>Status<select data-filter="status">${STATUS_OPTIONS.map((s) => `<option value="${s === 'All' ? '' : s}">${s}</option>`).join('')}</select></label>
      <label>Search<input data-filter="search" type="search" placeholder="Session ID or user" /></label>
    </div>
    <p class="status-line" data-role="picker-status">Loading saved handover sessions…</p>
    <div class="picker-results" data-role="picker-results"></div>
    <div class="session-actions-row"><button class="btn btn-ghost" data-nav="sessionOpen">Back</button></div>
  </section>`;

  const statusEl = root.querySelector('[data-role="picker-status"]');
  const resultsEl = root.querySelector('[data-role="picker-results"]');
  const shiftInput = root.querySelector('[data-filter="shift"]');
  const dateInput = root.querySelector('[data-filter="date"]');
  const statusInput = root.querySelector('[data-filter="status"]');
  const searchInput = root.querySelector('[data-filter="search"]');

  shiftInput.value = selectedShift;

  const renderList = (items) => {
    if (!items.length) {
      resultsEl.innerHTML = '<article class="picker-empty">No saved sessions match the current filters.</article>';
      return;
    }
    resultsEl.innerHTML = items.map((session) => `<article class="picker-session-card" data-session-id="${session.sessionId}">
      <header><strong>${text(session.shiftCode)} · ${text(session.shiftLabel || mapShiftLabel(session.shiftCode))}</strong><span>${text(normaliseDate(session.shiftDate))}</span></header>
      <p>Status: ${text(session.sessionStatus)}</p>
      <p>Last updated: ${text(session.updatedAt || session.createdAt)}</p>
      <p>Updated by: ${text(session.updatedBy || session.createdBy)}</p>
      <p class="picker-debug">Session ID: ${text(session.sessionId)}</p>
      <button class="btn btn-primary" data-action="open-session" data-session-id="${session.sessionId}">Open Session</button>
    </article>`).join('');

    resultsEl.querySelectorAll('[data-action="open-session"]').forEach((button) => button.addEventListener('click', async () => {
      const sessionId = Number(button.dataset.sessionId);
      button.disabled = true;
      statusEl.className = 'status-line';
      statusEl.textContent = `Loading session ${sessionId}…`;
      try {
        const payload = await sessionService.openSessionById(sessionId);
        validateSessionPayload(payload);
        applySessionPayload(payload);
        setActiveSessionContext({
          selectedShift: payload.shiftCode,
          selectedShiftLabel: mapShiftLabel(payload.shiftCode),
          activeSessionMode: 'open',
          activeSessionDate: payload.shiftDate,
          activeSessionStatus: payload.sessionStatus,
          activeSessionId: payload.sessionId,
          activeSessionSummary: { status: payload.sessionStatus, updatedAt: payload.updatedAt || payload.createdAt || null, updatedBy: payload.updatedBy || payload.createdBy || null }
        });
        window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'departmentBoard' } }));
      } catch (error) {
        button.disabled = false;
        statusEl.className = 'status-line warn';
        statusEl.textContent = error instanceof Error ? error.message : 'Unable to open session.';
      }
    }));
  };

  let allItems = [];
  const applyFilters = () => {
    const shift = shiftInput.value;
    const date = dateInput.value;
    const status = statusInput.value;
    const search = searchInput.value.trim().toLowerCase();
    const filtered = allItems.filter((item) => {
      if (shift && item.shiftCode !== shift) return false;
      if (date && normaliseDate(item.shiftDate) !== date) return false;
      if (status && item.sessionStatus !== status) return false;
      if (search) {
        const hay = `${item.sessionId} ${item.updatedBy || ''} ${item.createdBy || ''}`.toLowerCase();
        if (!hay.includes(search)) return false;
      }
      return true;
    });
    statusEl.className = 'status-line';
    statusEl.textContent = `Showing ${filtered.length} saved session(s).`;
    renderList(filtered);
  };

  [shiftInput, dateInput, statusInput, searchInput].forEach((el) => el.addEventListener('input', applyFilters));

  root.querySelectorAll('[data-nav]').forEach((button) => button.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: button.dataset.nav } }));
  }));

  (async () => {
    try {
      allItems = await sessionService.listSessions({ shiftCode: selectedShift });
      if (!Array.isArray(allItems)) throw new Error('Invalid session list response.');
      applyFilters();
    } catch (error) {
      statusEl.className = 'status-line warn';
      statusEl.textContent = error instanceof Error ? `Unable to load saved sessions. ${error.message}` : 'Unable to load saved sessions.';
      resultsEl.innerHTML = '<article class="picker-empty">Could not load sessions. Check runtime bridge and try again.</article>';
    }
  })();
}
