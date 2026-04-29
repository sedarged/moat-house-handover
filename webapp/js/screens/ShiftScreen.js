import { sessionService } from '../services/sessionService.js';
import { applySessionPayload } from '../state/appState.js';
import { SHIFT_TIMES } from '../models/contracts.js';
import { iconBrandSvg, iconBell, iconClock, iconCalendar, iconUser, iconHistory, iconFolder, iconChart, iconChevron } from '../core/icons.js';

function todayIsoDate() {
  const now = new Date();
  const yyyy = now.getFullYear();
  const mm = `${now.getMonth() + 1}`.padStart(2, '0');
  const dd = `${now.getDate()}`.padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

function formatDateDisplay(isoDate) {
  if (!isoDate) return '';
  try {
    const [y, m, d] = isoDate.split('-');
    return `${d}/${m}/${y}`;
  } catch {
    return isoDate;
  }
}

export function renderShiftScreen(root, state) {
  const today = todayIsoDate();

  root.innerHTML = '';

  const screen = document.createElement('div');
  screen.className = 'screen';

  /* ── HEADER ── */
  screen.innerHTML = `
    <header class="screen-header">
      <div class="header-brand">
        ${iconBrandSvg}
        <div class="header-brand-text">
          <span class="header-brand-sub">Moat House</span>
          <span class="header-brand-name">Operations</span>
        </div>
      </div>
      <div class="header-title">MOAT HOUSE HANDOVER</div>
      <div class="header-actions">
        <button class="header-icon-btn" id="hdr-bell" title="Notifications" type="button">
          ${iconBell}
          <span class="header-dot"></span>
        </button>
        <span class="header-divider"></span>

      </div>
    </header>

    <div class="screen-infobar">
      <div class="infobar-item">
        <span class="infobar-icon">${iconCalendar}</span>
        <span>Date: ${formatDateDisplay(today)}</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconBrandSvg}</span>
        <span>Site: Moat House</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconUser}</span>
        <span id="infobar-user">User: —</span>
      </div>
      <div class="infobar-item">
        <span class="infobar-icon">${iconHistory}</span>
        <span id="infobar-last-session">Last session: —</span>
      </div>
    </div>

    <div class="screen-content">
      <div class="shift-welcome">
        <div class="shift-welcome-icon">${iconFolderLarge}</div>
        <div class="shift-welcome-title" id="welcome-title">Welcome</div>
        <div class="shift-welcome-sub">Select a shift to open the handover dashboard.</div>
      </div>

      <div class="shift-cards-row">

        <div class="shift-card shift-am">
          <div class="shift-card-icon-wrap">${iconClock}</div>
          <div class="shift-card-name">AM SHIFT</div>
          <div class="shift-card-time">${SHIFT_TIMES.AM}</div>
          <div class="shift-ready-badge">
            <span class="shift-ready-dot"></span>Ready
          </div>
          <button class="btn btn-primary" data-shift="AM" type="button">Open</button>
        </div>

        <div class="shift-card shift-pm">
          <div class="shift-card-icon-wrap">${iconClock}</div>
          <div class="shift-card-name">PM SHIFT</div>
          <div class="shift-card-time">${SHIFT_TIMES.PM}</div>
          <div class="shift-ready-badge">
            <span class="shift-ready-dot"></span>Ready
          </div>
          <button class="btn btn-primary" data-shift="PM" type="button">Open</button>
        </div>

        <div class="shift-card shift-ns">
          <div class="shift-card-icon-wrap">${iconClock}</div>
          <div class="shift-card-name">NIGHT SHIFT</div>
          <div class="shift-card-time">${SHIFT_TIMES.NS}</div>
          <div class="shift-ready-badge">
            <span class="shift-ready-dot"></span>Ready
          </div>
          <button class="btn btn-primary" data-shift="NS" type="button">Open</button>
        </div>

      </div>

      <div class="quick-actions-row">
        <div class="quick-action-card" id="qa-continue">
          <span class="quick-action-icon">${iconHistory}</span>
          <span class="quick-action-text">Continue Last Session</span>
          <span class="quick-action-chevron">${iconChevron}</span>
        </div>
        <div class="quick-action-card" id="qa-review">
          <span class="quick-action-icon">${iconFolder}</span>
          <span class="quick-action-text">Review Previous Handovers</span>
          <span class="quick-action-chevron">${iconChevron}</span>
        </div>
        <div class="quick-action-card" id="qa-budget">
          <span class="quick-action-icon">${iconChart}</span>
          <span class="quick-action-text">Budget Summary</span>
          <span class="quick-action-chevron">${iconChevron}</span>
        </div>
      </div>

      <p class="status-line" id="shift-message"></p>

      <!-- Hidden date/user inputs for session creation -->
      <div style="display:none">
        <input type="date" id="hidden-date" value="${today}" />
        <input type="text"  id="hidden-user" value="${state.session?.userName || ''}" />
      </div>
    </div>

    <footer class="screen-footer">
      <button class="btn btn-ghost" id="footer-help"     type="button">? &nbsp;Help</button>
      <button class="btn btn-ghost" id="footer-exit"     type="button">Exit</button>
    </footer>
  `;

  root.append(screen);

  const message       = screen.querySelector('#shift-message');
  const welcomeTitle  = screen.querySelector('#welcome-title');
  const infobarUser   = screen.querySelector('#infobar-user');
  const infobarLast   = screen.querySelector('#infobar-last-session');
  const hiddenDate    = screen.querySelector('#hidden-date');
  const hiddenUser    = screen.querySelector('#hidden-user');

  /* ── populate user from appState ── */
  if (state.session?.userName) {
    welcomeTitle.textContent = `Welcome, ${state.session.userName}`;
    infobarUser.textContent  = `User: ${state.session.userName}`;
  }

  /* try to get windows username from host */
  getRuntimeUserName().then((name) => {
    if (name) {
      welcomeTitle.textContent    = `Welcome, ${name}`;
      infobarUser.textContent     = `User: ${name}`;
      hiddenUser.value            = name;
    }
  }).catch(() => {});

  /* ── populate last session info ── */
  if (state.session?.sessionId && state.session?.shiftCode && state.session?.shiftDate) {
    infobarLast.textContent = `Last session: ${state.session.shiftCode} ${formatDateDisplay(state.session.shiftDate)}`;
  }

  /* ── open shift card buttons ── */
  screen.querySelectorAll('[data-shift]').forEach((btn) => {
    btn.addEventListener('click', async () => {
      const shiftCode = btn.dataset.shift;
      const shiftDate = hiddenDate.value || todayIsoDate();
      const userName  = hiddenUser.value.trim() || 'User';
      await openSession(shiftCode, shiftDate, userName, message);
    });
  });

  /* ── quick actions ── */
  screen.querySelector('#qa-continue')?.addEventListener('click', () => {
    if (state.session?.sessionId) {
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
    } else {
      message.textContent = 'No previous session loaded. Open a shift first.';
    }
  });

  screen.querySelector('#qa-review')?.addEventListener('click', () => {
    message.textContent = 'Review Previous Handovers: navigate to a saved session via Open Session.';
  });

  screen.querySelector('#qa-budget')?.addEventListener('click', () => {
    if (state.session?.sessionId) {
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'budget' } }));
    } else {
      message.textContent = 'Open a shift session first to access Budget Summary.';
    }
  });

  /* ── header buttons ── */
  screen.querySelector('#hdr-bell')?.addEventListener('click', () => {
    message.textContent = 'No new notifications.';
  });

  /* ── footer ── */
  screen.querySelector('#footer-help')?.addEventListener('click', () => {
    message.textContent = 'Run Diagnostics first on a new workstation. Select a shift, confirm the date, and press Open.';
  });
  screen.querySelector('#footer-exit')?.addEventListener('click', () => {
    message.textContent = 'Use the window controls to close the application.';
  });
}

async function getRuntimeUserName() {
  try {
    const { getRuntimeStatus } = await import('../core/hostBridge.js');
    const status = await getRuntimeStatus();
    return status?.currentUser || null;
  } catch {
    return null;
  }
}

async function openSession(shiftCode, shiftDate, userName, message) {
  message.textContent = 'Checking session...';
  message.className   = 'status-line';

  try {
    const openResult = await sessionService.openSession(shiftCode, shiftDate, userName);
    if (openResult.found && openResult.session) {
      applySessionPayload(openResult.session);
      message.textContent = `Loaded session #${openResult.session.sessionId}.`;
      message.className   = 'status-line success';
      window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
      return;
    }

    const confirmed = window.confirm(
      `No ${shiftCode} session exists for ${shiftDate}.\nCreate a blank session now?`
    );
    if (!confirmed) {
      message.textContent = 'No session opened.';
      message.className   = 'status-line';
      return;
    }

    message.textContent = 'Creating blank session...';
    const createResult = await sessionService.createBlankSession(shiftCode, shiftDate, userName);
    applySessionPayload(createResult.session);
    message.textContent = `Created session #${createResult.session.sessionId}.`;
    message.className   = 'status-line success';
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'dashboard' } }));
  } catch (err) {
    message.textContent = err instanceof Error ? err.message : 'Unexpected session error.';
    message.className   = 'status-line error';
  }
}

/* inline icon for the large clipboard welcome icon */
const iconFolderLarge = `<svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2" stroke-linecap="round" stroke-linejoin="round">
  <rect x="3" y="3" width="7" height="7" rx="1"/>
  <rect x="14" y="3" width="7" height="7" rx="1"/>
  <rect x="3" y="14" width="7" height="7" rx="1"/>
  <path d="M14 17h7M17.5 14v6"/>
</svg>`;
