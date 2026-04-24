import { routes, renderRoute } from './core/router.js';
import { initializeHostBridge, getRuntimeStatus } from './core/hostBridge.js';
import { appState, setActiveDepartmentName } from './state/appState.js';

const root = document.getElementById('screen-root');
const navButtons = document.querySelectorAll('[data-route]');
const appContext = document.getElementById('app-context');

function refreshContext() {
  if (appState.session?.sessionId) {
    appContext.textContent = `Session #${appState.session.sessionId} • ${appState.session.shiftCode} • ${appState.session.shiftDate} • ${appState.session.sessionStatus}`;
    return;
  }

  appContext.textContent = 'Runtime ready • open a shift session';
}

function navigate(routeName) {
  const route = routes[routeName] ? routeName : 'shift';
  appState.currentRoute = route;
  renderRoute(route, root, appState);
  refreshContext();
}

window.addEventListener('app:navigate', (event) => {
  if (event.detail?.deptName) {
    setActiveDepartmentName(event.detail.deptName);
  }

  navigate(event.detail?.route || 'shift');
});

navButtons.forEach((button) => {
  button.addEventListener('click', () => navigate(button.dataset.route));
});

initializeHostBridge();

getRuntimeStatus()
  .then((status) => {
    appContext.textContent = `Runtime ready • DB: ${status.accessDatabasePath}`;
  })
  .catch(() => {
    appContext.textContent = 'Stage 2B runtime bridge unavailable in browser mode';
  });

navigate('shift');
