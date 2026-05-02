import { renderHomeScreen } from '../screens/HomeScreen.js';
import { renderModuleScreen } from '../screens/ModuleScreen.js';

export const routes = {
  home: { label: 'Home', renderer: renderHomeScreen },
  am: { label: 'AM Handover', renderer: (root, state) => renderModuleScreen(root, state, { title: 'AM Handover', phase: 'Phase 10D', accent: 'am' }) },
  pm: { label: 'PM Handover', renderer: (root, state) => renderModuleScreen(root, state, { title: 'PM Handover', phase: 'Phase 10D', accent: 'pm' }) },
  night: { label: 'Night Shift', renderer: (root, state) => renderModuleScreen(root, state, { title: 'Night Shift Handover', phase: 'Phase 10D', accent: 'ns' }) },
  reports: { label: 'Reports / Preview', renderer: (root, state) => renderModuleScreen(root, state, { title: 'Reports / Preview', phase: 'Phase 10I' }) },
  budget: { label: 'Budget', renderer: (root, state) => renderModuleScreen(root, state, { title: 'Budget', phase: 'Phase 10G' }) },
  attachments: { label: 'Attachments', renderer: (root, state) => renderModuleScreen(root, state, { title: 'Attachments', phase: 'Phase 10H' }) },
  history: { label: 'History / Search', renderer: (root, state) => renderModuleScreen(root, state, { title: 'History / Search', phase: 'Future phase' }) },
  admin: { label: 'Admin / Diagnostics', adminOnly: true, renderer: (root, state) => renderModuleScreen(root, state, { title: 'Admin / Diagnostics', phase: 'Phase 10J', adminOnly: true }) },
  settings: { label: 'Settings', adminOnly: true, renderer: (root, state) => renderModuleScreen(root, state, { title: 'Settings', phase: 'Phase 10J', adminOnly: true }) }
};

export const navRoutes = Object.entries(routes).filter(([, cfg]) => !cfg.adminOnly);

export function renderRoute(routeName, root, state) {
  const route = routes[routeName] ? routeName : 'home';
  routes[route].renderer(root, state);
}
