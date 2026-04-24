import { routes, renderRoute } from './core/router.js';
import { appState } from './state/appState.js';

const root = document.getElementById('screen-root');
const navButtons = document.querySelectorAll('[data-route]');

function navigate(routeName) {
  const route = routes[routeName] ? routeName : 'shift';
  appState.currentRoute = route;
  renderRoute(route, root, appState);
}

navButtons.forEach((button) => {
  button.addEventListener('click', () => navigate(button.dataset.route));
});

navigate('shift');
