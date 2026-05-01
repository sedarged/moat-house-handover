import { routes, renderRoute } from './core/router.js';
import { initializeHostBridge, getRuntimeStatus } from './core/hostBridge.js';
import { appState, setRuntimeStatus, setRuntimeStatusError } from './state/appState.js';

const root = document.getElementById('screen-root');

function routeTo(route) {
  const next = routes[route] ? route : 'home';
  appState.currentRoute = next;
  document.querySelector('#content-root').innerHTML = '';
  renderRoute(next, document.querySelector('#content-root'), appState);
  document.querySelectorAll('.nav-item').forEach((n) => n.classList.toggle('active', n.dataset.route === next));
  const shift = next === 'am' ? 'AM' : next === 'pm' ? 'PM' : next === 'night' ? 'NS' : 'None';
  document.getElementById('active-shift').textContent = shift;
}

function renderShell() {
  root.innerHTML = `<div class="app-shell">
    <header class="app-header"><div><strong>MOAT HOUSE HANDOVER</strong><div class="date">${new Date().toLocaleDateString()}</div></div><div>Active shift: <span id="active-shift">None</span></div></header>
    <div class="shell-body"><nav class="app-nav">${Object.entries(routes).map(([key, cfg]) => `<button class="nav-item" data-route="${key}">${cfg.label}</button>`).join('')}</nav><main id="content-root" class="content-root"></main></div>
    <footer class="app-status">Provider: <span id="provider-pill">Loading...</span> | Lock: <span id="lock-pill">Loading...</span></footer>
  </div>`;
  root.querySelectorAll('.nav-item').forEach((item) => item.addEventListener('click', () => routeTo(item.dataset.route)));
}

window.addEventListener('app:navigate', (event) => routeTo(event.detail?.route || 'home'));
initializeHostBridge();
renderShell();

getRuntimeStatus().then((status) => {
  setRuntimeStatus(status);
  document.getElementById('provider-pill').textContent = status.effectiveProvider;
  document.getElementById('lock-pill').textContent = status.appLockStatus;
  routeTo(appState.currentRoute);
}).catch((err) => {
  setRuntimeStatusError(err?.message || 'Host bridge unavailable');
  document.getElementById('provider-pill').textContent = 'Status unavailable';
  document.getElementById('lock-pill').textContent = 'Status unavailable';
  routeTo(appState.currentRoute);
});
