import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('dashboard renders seeded session and budget summary through mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'dashboard');

  await expect(page.getByText(/DASHBOARD|HANDOVER/i)).toBeVisible();
  await expect(page.getByText('Injection')).toBeVisible();
  await expect(page.getByText('MetaPress')).toBeVisible();
  await expect(page.getByText(/Departments Completed/i)).toBeVisible();
  await expect(page.getByText(/Budget/i)).toBeVisible();
  await expect(page.getByText(/under/i)).toBeVisible();
});
