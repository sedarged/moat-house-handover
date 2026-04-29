import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('shift screen loads in browser with mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'shift');
  await expect(page.locator('#screen-root')).toBeVisible();
  await expect(page.getByText(/MOAT HOUSE HANDOVER/i)).toBeVisible();
});
