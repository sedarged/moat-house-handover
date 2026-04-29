import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('dashboard renders seeded session and budget summary through mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'dashboard');

  const screenRoot = page.locator('#screen-root');
  await expect(screenRoot).toContainText('Injection');
  await expect(screenRoot).toContainText('MetaPress');
  await expect(screenRoot).toContainText('Departments Completed');

  const budgetSummary = page.locator('.summary-side-item').filter({ hasText: 'Budget' }).first();
  await expect(budgetSummary).toBeVisible();
  await expect(budgetSummary).toContainText('under');
});
