import { applySessionPayload, setActiveSessionContext } from '../state/appState.js';
import { sessionService } from '../services/sessionService.js';

const STATUS_OPTIONS = ['All', 'Open', 'Draft', 'Closed'];

function text(value, fallback = '—') { return value == null || value === '' ? fallback : String(value); }
function normaliseDate(value) { return String(value || '').split('T')[0]; }

function createElement(tagName, className, value) {
  const element = document.createElement(tagName);
  if (className) element.className = className;
  if (value !== undefined && value !== null) element.textContent = String(value);
  return element;
}

function createButton(className, label) {
  const button = createElement('button', className, label);
  button.type = 'button';
  return button;
}

function createOption(value, label) {
  const option = createElement('option', null, label);
  option.value = value;
  return option;
}

function createLabeledControl(labelText, control) {
  const label = createElement('label');
  label.append(document.createTextNode(labelText), control);
  return label;
}

function validateSessionPayload(payload) {
  const required = ['sessionId', 'shiftCode', 'shiftDate', 'sessionStatus', 'departments'];
  const missing = required.filter((key) => payload?.[key] == null);
  if (missing.length > 0) throw new Error(`Session payload missing required fields: ${missing.join(', ')}`);
}

function mapShiftLabel(code) {
  if (!code) return 'All shifts';
  return code === 'NS' ? 'Night Shift' : `${code} Shift`;
}

function navigate(route) {
  window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route } }));
}

export function renderExistingHandoverPickerScreen(root, state) {
  const selectedShift = state.selectedShift || '';

  const section = createElement('section', 'existing-handover-picker');

  const header = createElement('div', 'session-header-card');
  header.append(
    createElement('p', 'session-kicker', mapShiftLabel(selectedShift)),
    createElement('h2', null, 'Open Existing Handover'),
    createElement('p', null, 'Select a saved handover session and load it into the active workflow.')
  );
  section.append(header);

  const filters = createElement('div', 'picker-filters');

  const shiftInput = document.createElement('select');
  shiftInput.dataset.filter = 'shift';
  shiftInput.append(
    createOption('', 'All'),
    createOption('AM', 'AM'),
    createOption('PM', 'PM'),
    createOption('NS', 'NS')
  );
  shiftInput.value = selectedShift;

  const dateInput = document.createElement('input');
  dateInput.dataset.filter = 'date';
  dateInput.type = 'date';

  const statusInput = document.createElement('select');
  statusInput.dataset.filter = 'status';
  STATUS_OPTIONS.forEach((status) => statusInput.append(createOption(status === 'All' ? '' : status, status)));

  const searchInput = document.createElement('input');
  searchInput.dataset.filter = 'search';
  searchInput.type = 'search';
  searchInput.placeholder = 'Session ID or user';

  filters.append(
    createLabeledControl('Shift', shiftInput),
    createLabeledControl('Date', dateInput),
    createLabeledControl('Status', statusInput),
    createLabeledControl('Search', searchInput)
  );
  section.append(filters);

  const statusEl = createElement('p', 'status-line', 'Loading saved handover sessions…');
  statusEl.dataset.role = 'picker-status';
  section.append(statusEl);

  const resultsEl = createElement('div', 'picker-results');
  resultsEl.dataset.role = 'picker-results';
  section.append(resultsEl);

  const actions = createElement('div', 'session-actions-row');
  const backButton = createButton('btn btn-ghost', 'Back');
  backButton.dataset.nav = 'sessionOpen';
  actions.append(backButton);
  section.append(actions);

  root.replaceChildren(section);

  const renderEmpty = (message) => {
    resultsEl.replaceChildren(createElement('article', 'picker-empty', message));
  };

  const createSessionCard = (session) => {
    const sessionId = String(session?.sessionId ?? '');
    const article = createElement('article', 'picker-session-card');
    article.dataset.sessionId = sessionId;

    const cardHeader = createElement('header');
    const heading = createElement('strong', null, `${text(session?.shiftCode)} · ${text(session?.shiftLabel || mapShiftLabel(session?.shiftCode))}`);
    const date = createElement('span', null, text(normaliseDate(session?.shiftDate)));
    cardHeader.append(heading, date);

    const status = createElement('p', null, `Status: ${text(session?.sessionStatus)}`);
    const updated = createElement('p', null, `Last updated: ${text(session?.updatedAt || session?.createdAt)}`);
    const user = createElement('p', null, `Updated by: ${text(session?.updatedBy || session?.createdBy)}`);
    const debug = createElement('p', 'picker-debug', `Session ID: ${text(session?.sessionId)}`);
    const openButton = createButton('btn btn-primary', 'Open Session');
    openButton.dataset.action = 'open-session';
    openButton.dataset.sessionId = sessionId;
    openButton.disabled = !sessionId;

    openButton.addEventListener('click', async () => {
      openButton.disabled = true;
      statusEl.className = 'status-line';
      statusEl.textContent = `Loading session ${text(sessionId)}…`;
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
          activeSessionSummary: {
            status: payload.sessionStatus,
            updatedAt: payload.updatedAt || payload.createdAt || null,
            updatedBy: payload.updatedBy || payload.createdBy || null
          }
        });
        navigate('departmentBoard');
      } catch (error) {
        openButton.disabled = false;
        statusEl.className = 'status-line warn';
        statusEl.textContent = error instanceof Error ? error.message : 'Unable to open session.';
      }
    });

    article.append(cardHeader, status, updated, user, debug, openButton);
    return article;
  };

  const renderList = (items) => {
    if (!items.length) {
      renderEmpty('No saved sessions match the current filters.');
      return;
    }
    resultsEl.replaceChildren(...items.map(createSessionCard));
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

  backButton.addEventListener('click', () => navigate(backButton.dataset.nav));

  (async () => {
    try {
      const initialFilters = selectedShift ? { shiftCode: selectedShift } : {};
      allItems = await sessionService.listSessions(initialFilters);
      if (!Array.isArray(allItems)) throw new Error('Invalid session list response.');
      applyFilters();
    } catch (error) {
      statusEl.className = 'status-line warn';
      statusEl.textContent = error instanceof Error ? `Unable to load saved sessions. ${error.message}` : 'Unable to load saved sessions.';
      renderEmpty('Could not load sessions. Check runtime bridge and try again.');
    }
  })();
}
