import { routes, renderRoute } from './core/router.js';
import { initializeHostBridge, getRuntimeStatus } from './core/hostBridge.js';
import { appState, applySessionPayload, setActiveDepartmentName, setSelectedAttachmentId } from './state/appState.js';

const root = document.getElementById('screen-root');

/* expose helpers on window for diagnostics / integration testing */
window.__mhApp = { appState, applySessionPayload, navigate: (r) => navigate(r) };

function navigate(routeName) {
  const route = routes[routeName] ? routeName : 'shift';
  appState.currentRoute = route;
  renderRoute(route, root, appState);
}

window.addEventListener('app:navigate', (event) => {
  if (event.detail?.deptName) {
    setActiveDepartmentName(event.detail.deptName);
  }
  if (event.detail?.attachmentId) {
    setSelectedAttachmentId(Number(event.detail.attachmentId));
  }
  navigate(event.detail?.route || 'shift');
});

initializeHostBridge();

getRuntimeStatus()
  .then(() => {})
  .catch(() => {});

navigate('shift');
