import { renderHomeScreen } from '../screens/HomeScreen.js';
import { renderModuleScreen } from '../screens/ModuleScreen.js';

export const routes = {
  home: { label: 'Home', renderer: renderHomeScreen },
  am: { label: 'AM Handover', renderer: (root, state) => renderModuleScreen(root, state, { key: 'am', title: 'AM Handover', phase: 'Phase 10D', accent: 'am' }) },
  pm: { label: 'PM Handover', renderer: (root, state) => renderModuleScreen(root, state, { key: 'pm', title: 'PM Handover', phase: 'Phase 10D', accent: 'pm' }) },
  night: { label: 'Night Shift', renderer: (root, state) => renderModuleScreen(root, state, { key: 'night', title: 'Night Shift Handover', phase: 'Phase 10D', accent: 'ns' }) },
  reports: { label: 'Reports', renderer: (root, state) => renderModuleScreen(root, state, { key: 'reports', title: 'Reports / Preview', phase: 'Phase 10I' }) },
  budget: { label: 'Budget', renderer: (root, state) => renderModuleScreen(root, state, { key: 'budget', title: 'Budget', phase: 'Phase 10G' }) },
  attachments: { label: 'Attachments', renderer: (root, state) => renderModuleScreen(root, state, { key: 'attachments', title: 'Attachments', phase: 'Phase 10H' }) },
  history: { label: 'History', renderer: (root, state) => renderModuleScreen(root, state, { key: 'history', title: 'History / Search', phase: 'Future phase' }) },
  admin: { label: 'Admin', renderer: (root, state) => renderModuleScreen(root, state, { key: 'admin', title: 'Admin / Diagnostics', phase: 'Phase 10J', adminOnly: true }) },
  settings: { label: 'Settings', renderer: (root, state) => renderModuleScreen(root, state, { key: 'settings', title: 'Settings', phase: 'Phase 10J', adminOnly: true }) }
};

export function renderRoute(routeName, root, state) {
  const route = routes[routeName] ? routeName : 'home';
  root.innerHTML = '';
  routes[route].renderer(root, state);
}
