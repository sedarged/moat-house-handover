import { renderShiftScreen } from '../screens/ShiftScreen.js';
import { renderDashboardScreen } from '../screens/DashboardScreen.js';
import { renderDepartmentScreen } from '../screens/DepartmentScreen.js';
import { renderBudgetScreen } from '../screens/BudgetScreen.js';
import { renderPreviewScreen } from '../screens/PreviewScreen.js';
import { renderImageViewerScreen } from '../screens/ImageViewer.js';
import { renderSendScreen } from '../screens/SendScreen.js';
import { renderDiagnosticsScreen } from '../screens/DiagnosticsScreen.js';
import { renderHomeScreen } from '../screens/HomeScreen.js';
import { renderModuleScreen } from '../screens/ModuleScreen.js';

export const routes = {
  home: { label: 'Home', renderer: renderHomeScreen },
  am: { label: 'AM Handover', renderer: (root, state) => renderModuleScreen(root, state, { key: 'am-handover', title: 'AM Handover', phase: 'Phase 10D', accent: 'am' }) },
  pm: { label: 'PM Handover', renderer: (root, state) => renderModuleScreen(root, state, { key: 'pm-handover', title: 'PM Handover', phase: 'Phase 10D', accent: 'pm' }) },
  night: { label: 'Night Shift', renderer: (root, state) => renderModuleScreen(root, state, { key: 'night-handover', title: 'Night Shift Handover', phase: 'Phase 10D', accent: 'ns' }) },
  reports: { label: 'Reports / Preview', renderer: renderPreviewScreen },
  budgetMenu: { label: 'Budget', renderer: renderBudgetScreen },
  attachments: { label: 'Attachments', renderer: (root, state) => renderModuleScreen(root, state, { key: 'attachments', title: 'Attachments', phase: 'Phase 10H' }) },
  history: { label: 'History / Search', renderer: (root, state) => renderModuleScreen(root, state, { key: 'history', title: 'History / Search', phase: 'Future phase' }) },
  admin: { label: 'Admin / Diagnostics', adminOnly: true, renderer: renderDiagnosticsScreen },
  settings: { label: 'Settings', adminOnly: true, renderer: (root, state) => renderModuleScreen(root, state, { key: 'settings', title: 'Settings', phase: 'Phase 10J', adminOnly: true }) },

  shift: { label: 'Shift', legacy: true, renderer: renderShiftScreen },
  dashboard: { label: 'Dashboard', legacy: true, renderer: renderDashboardScreen },
  department: { label: 'Department', legacy: true, renderer: renderDepartmentScreen },
  budget: { label: 'Budget (Legacy)', legacy: true, renderer: renderBudgetScreen },
  preview: { label: 'Preview (Legacy)', legacy: true, renderer: renderPreviewScreen },
  viewer: { label: 'Viewer', legacy: true, renderer: renderImageViewerScreen },
  send: { label: 'Send', legacy: true, renderer: renderSendScreen },
  diagnostics: { label: 'Diagnostics (Legacy)', legacy: true, renderer: renderDiagnosticsScreen }
};

export const navRoutes = Object.entries(routes).filter(([, cfg]) => !cfg.adminOnly && !cfg.legacy);

export function renderRoute(routeName, root, state) {
  const route = routes[routeName] ? routeName : 'home';
  root.innerHTML = '';
  routes[route].renderer(root, state);
}
