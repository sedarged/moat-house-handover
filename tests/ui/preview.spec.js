import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('preview renders persisted session, department and budget data through mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'preview');

  await expect(page.getByText(/PREVIEW/i)).toBeVisible();
  await expect(page.getByText('Injection')).toBeVisible();
  await expect(page.getByText('MetaPress')).toBeVisible();
  await expect(page.getByText(/Budget/i)).toBeVisible();
  await expect(page.getByText(/Short on FP/i)).toBeVisible();
});
