import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('Department Detail save refreshes Department Status Board state', async ({ page }) => {
  await openMockRoute(page, 'departmentBoard');

  const board = page.locator('.department-board');
  const card = board.locator('.department-card').filter({ hasText: 'Injection' });
  await expect(card.locator('.status-pill')).toHaveText('Completed');

  await card.getByRole('button', { name: 'Open Department' }).click();

  const detail = page.locator('.department-detail-editor');
  await expect(detail.getByRole('heading', { name: 'DEPARTMENT DETAIL', exact: true })).toBeVisible();

  await detail.locator('select').first().selectOption('Incomplete');
  await detail.getByRole('button', { name: 'Save Department' }).click();
  await expect(detail.getByText('Department saved.', { exact: true })).toBeVisible();

  await detail.locator('.screen-footer')
    .getByRole('button', { name: 'Back to Department Status Board' })
    .click();

  await expect(
    page.locator('.department-board .department-card').filter({ hasText: 'Injection' }).locator('.status-pill')
  ).toHaveText('Incomplete');
});
