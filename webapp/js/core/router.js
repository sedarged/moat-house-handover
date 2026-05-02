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
import { renderShiftDashboardScreen } from '../screens/ShiftDashboardScreen.js';
import { renderHandoverSessionScreen } from '../screens/HandoverSessionScreen.js';
import { renderDepartmentStatusBoardScreen } from '../screens/DepartmentStatusBoardScreen.js';
import { renderDepartmentDetailEntryScreen } from '../screens/DepartmentDetailEntryScreen.js';

export const routes = {
  home: { label: 'Home', renderer: renderHomeScreen },
  am: { label: 'AM Handover', renderer: (root, state) => renderShiftDashboardScreen(root, state, { shiftCode: 'AM', title: 'AM Shift Handover', label: 'AM Shift', hours: '06:00 – 14:00', accent: 'am', statusLabel: 'Ready', description: 'Start AM handover tasks, continue draft progress, and review readiness before sign-off.' }) },
  pm: { label: 'PM Handover', renderer: (root, state) => renderShiftDashboardScreen(root, state, { shiftCode: 'PM', title: 'PM Shift Handover', label: 'PM Shift', hours: '14:00 – 22:00', accent: 'pm', statusLabel: 'Draft not started', description: 'Prepare PM shift updates, continue draft entries, and open workflow modules from one dashboard.' }) },
  night: { label: 'Night Shift', renderer: (root, state) => renderShiftDashboardScreen(root, state, { shiftCode: 'NS', title: 'Night Shift Handover', label: 'Night Shift', hours: '22:00 – 06:00', accent: 'ns', statusLabel: 'No active handover yet', description: 'Use Night dashboard to launch handover workflow, track readiness, and review carry-over items.' }) },

  sessionCreate: { label: 'Session Create', renderer: (root, state) => renderHandoverSessionScreen(root, state, { mode: 'create', shiftCode: state.selectedShift || 'AM', shiftLabel: state.selectedShiftLabel || `${state.selectedShift || 'AM'} Shift`, date: state.activeSessionDate || new Date().toLocaleDateString(), accent: (state.selectedShift || 'AM').toLowerCase() === 'ns' ? 'ns' : (state.selectedShift || 'AM').toLowerCase(), hours: state.selectedShift === 'PM' ? '14:00 – 22:00' : state.selectedShift === 'NS' ? '22:00 – 06:00' : '06:00 – 14:00', dashboardRoute: state.selectedShift === 'PM' ? 'pm' : state.selectedShift === 'NS' ? 'night' : 'am' }) },
  sessionContinue: { label: 'Session Continue', renderer: (root, state) => renderHandoverSessionScreen(root, state, { mode: 'continue', shiftCode: state.selectedShift || 'AM', shiftLabel: state.selectedShiftLabel || `${state.selectedShift || 'AM'} Shift`, date: state.activeSessionDate || new Date().toLocaleDateString(), accent: (state.selectedShift || 'AM').toLowerCase() === 'ns' ? 'ns' : (state.selectedShift || 'AM').toLowerCase(), hours: state.selectedShift === 'PM' ? '14:00 – 22:00' : state.selectedShift === 'NS' ? '22:00 – 06:00' : '06:00 – 14:00', dashboardRoute: state.selectedShift === 'PM' ? 'pm' : state.selectedShift === 'NS' ? 'night' : 'am' }) },
  sessionOpen: { label: 'Session Open', renderer: (root, state) => renderHandoverSessionScreen(root, state, { mode: 'open', shiftCode: state.selectedShift || 'AM', shiftLabel: state.selectedShiftLabel || `${state.selectedShift || 'AM'} Shift`, date: state.activeSessionDate || new Date().toLocaleDateString(), accent: (state.selectedShift || 'AM').toLowerCase() === 'ns' ? 'ns' : (state.selectedShift || 'AM').toLowerCase(), hours: state.selectedShift === 'PM' ? '14:00 – 22:00' : state.selectedShift === 'NS' ? '22:00 – 06:00' : '06:00 – 14:00', dashboardRoute: state.selectedShift === 'PM' ? 'pm' : state.selectedShift === 'NS' ? 'night' : 'am' }) },

  departmentBoard: { label: 'Department Board', renderer: (root, state) => renderDepartmentStatusBoardScreen(root, state, { shiftCode: state.selectedShift || 'AM', shiftLabel: state.selectedShiftLabel || `${state.selectedShift || 'AM'} Shift`, sessionDate: state.activeSessionDate || new Date().toLocaleDateString(), sessionMode: state.activeSessionMode, sessionStatus: state.activeSessionStatus, accent: (state.selectedShift || 'AM').toLowerCase() === 'ns' ? 'ns' : (state.selectedShift || 'AM').toLowerCase() }) },
  departmentDetailEntry: { label: 'Department Detail Entry', renderer: renderDepartmentDetailEntryScreen },

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
