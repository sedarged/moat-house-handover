import { renderShiftScreen } from '../screens/ShiftScreen.js';
import { renderDashboardScreen } from '../screens/DashboardScreen.js';
import { renderDepartmentScreen } from '../screens/DepartmentScreen.js';
import { renderBudgetScreen } from '../screens/BudgetScreen.js';
import { renderPreviewScreen } from '../screens/PreviewScreen.js';
import { renderImageViewerScreen } from '../screens/ImageViewer.js';
import { renderSendScreen } from '../screens/SendScreen.js';

export const routes = {
  shift: renderShiftScreen,
  dashboard: renderDashboardScreen,
  department: renderDepartmentScreen,
  budget: renderBudgetScreen,
  preview: renderPreviewScreen,
  viewer: renderImageViewerScreen,
  send: renderSendScreen
};

export function renderRoute(routeName, root, state) {
  root.innerHTML = '';
  const renderer = routes[routeName] || routes.shift;
  renderer(root, state);
}
