import { routes, renderRoute } from './core/router.js';
import { initializeHostBridge, getRuntimeStatus } from './core/hostBridge.js';
import { appState } from './state/appState.js';

const root = document.getElementById('screen-root');
const navButtons = document.querySelectorAll('[data-route]');
const appContext = document.getElementById('app-context');

function navigate(routeName) {
  const route = routes[routeName] ? routeName : 'shift';
  appState.currentRoute = route;
  renderRoute(route, root, appState);
}

navButtons.forEach((button) => {
  button.addEventListener('click', () => navigate(button.dataset.route));
});

initializeHostBridge();

getRuntimeStatus()
  .then((status) => {
    appContext.textContent = `Runtime ready • DB: ${status.accessDatabasePath}`;
  })
  .catch(() => {
    appContext.textContent = 'Stage 2A Runtime (host bridge unavailable in browser mode)';
  });

navigate('shift');
