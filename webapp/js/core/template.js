export function placeholderTemplate(title, purpose, actions = []) {
  const actionsHtml = actions.map((action) => `<li>${action}</li>`).join('');

  return `
    <section class="placeholder">
      <h2>${title}</h2>
      <p>${purpose}</p>
      <p class="meta">Stage 1 placeholder with route and layout contract only.</p>
      <h3>Planned actions</h3>
      <ul>${actionsHtml}</ul>
    </section>
  `;
}
