export function renderModuleScreen(root, state, module) {
  root.innerHTML = `<section class="module-screen">
    <header><h1 class="accent-${module.accent || 'default'}">${module.title}</h1><p>${module.adminOnly ? 'Admin-only area.' : 'Module placeholder screen.'} Full workflow is scheduled for ${module.phase}.</p></header>
    <div class="module-actions"><button data-nav="home">Back to Home</button>${(module.key || '').includes('handover') ? '' : ''}</div>
    <div class="placeholder-card">
      <h3>What is implemented now</h3>
      <ul><li>App shell navigation route exists.</li><li>Module entry point is available.</li><li>Consistent status and layout styling applied.</li></ul>
      <h3>Planned next</h3>
      <p>Detailed feature implementation in ${module.phase}.</p>
    </div>
  </section>`;
  root.querySelector('[data-nav="home"]').addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('app:navigate', { detail: { route: 'home' } }));
  });
}
