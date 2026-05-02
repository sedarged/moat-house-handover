import { routes, navRoutes, renderRoute } from './core/router.js';
import { initializeHostBridge, getRuntimeStatus } from './core/hostBridge.js';
import { appState, setRuntimeStatus, setRuntimeStatusError, applySessionPayload, setActiveDepartmentName, setSelectedAttachmentId } from './state/appState.js';

const root = document.getElementById('screen-root');

function navigate(route) {
  const next = routes[route] ? route : 'home';
  appState.currentRoute = next;
  const contentRoot = document.querySelector('#content-root');
  if (!contentRoot) return;
  contentRoot.innerHTML = '';
  renderRoute(next, contentRoot, appState);
  document.querySelectorAll('.shell-nav-item').forEach((n) => n.classList.toggle('active', n.dataset.route === next));
  document.getElementById('active-shift-label').textContent = next === 'am' ? 'AM' : next === 'pm' ? 'PM' : next === 'night' ? 'NS' : 'None';
}

window.__mhApp = { appState, applySessionPayload, setActiveDepartmentName, setSelectedAttachmentId, navigate: (r) => navigate(r) };

function renderShell() {
  root.innerHTML = `<div class="shell-app">
<header class="shell-header"><div class="shell-brand">MOAT HOUSE OPERATIONS</div><div class="shell-title">MOAT HOUSE HANDOVER</div><div class="shell-header-icons"><button data-nav="admin">🔔</button><button data-nav="settings">⚙</button></div></header>
<div class="shell-infostrip"><span>Date: ${new Date().toLocaleDateString()}</span><span>Site: Moat House</span><span>User: Supervisor</span><span>Last session: PM ${new Date(Date.now()-86400000).toLocaleDateString()}</span><span>Active shift: <strong id="active-shift-label">None</strong></span></div>
<div class="shell-layout"><nav class="shell-nav">${navRoutes.map(([key,cfg])=>`<button class="shell-nav-item" data-route="${key}">${cfg.label}</button>`).join('')}</nav><main id="content-root" class="shell-content"></main></div>
<footer class="shell-status">Provider: <span id="provider-pill">Loading...</span> · Lock: <span id="lock-pill">Loading...</span> · Data root: <span id="root-pill">Loading...</span></footer>
</div>`;
  root.querySelectorAll('[data-route],[data-nav]').forEach((button) => {
    button.addEventListener('click', () => navigate(button.dataset.route || button.dataset.nav));
  });
}

window.addEventListener('app:navigate', (event) => {
  if (event.detail?.deptName) setActiveDepartmentName(event.detail.deptName);
  if (event.detail?.attachmentId) setSelectedAttachmentId(Number(event.detail.attachmentId));
  navigate(event.detail?.route || 'home');
});

initializeHostBridge();
renderShell();
getRuntimeStatus().then((status)=>{
  setRuntimeStatus(status);
  document.getElementById('provider-pill').textContent = status.effectiveProvider;
  document.getElementById('lock-pill').textContent = status.appLockStatus;
  document.getElementById('root-pill').textContent = status.approvedDataRoot;
  navigate(appState.currentRoute);
}).catch((err)=>{
  setRuntimeStatusError(err?.message || 'Host bridge unavailable');
  document.getElementById('provider-pill').textContent = 'Status unavailable';
  document.getElementById('lock-pill').textContent = 'Status unavailable';
  document.getElementById('root-pill').textContent = 'Status unavailable';
  navigate(appState.currentRoute);
});
