import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('budget screen renders seeded labour budget data and validates negative staff values', async ({ page }) => {
  await openMockRoute(page, 'budget');

  await expect(page.getByText('LABOUR BUDGET')).toBeVisible();
  await expect(page.getByText('Budget Summary')).toBeVisible();
  await expect(page.locator('#sum-lines-input')).toHaveValue('4');
  await expect(page.locator('#sum-register-input')).toHaveValue('46');
  await expect(page.getByText('Injection')).toBeVisible();
  await expect(page.getByText('MP / MetaPress')).toBeVisible();
  await expect(page.locator('tr[data-row-id="3"] textarea[name="reasonText"]')).toHaveValue('Agency Cover');
  await expect(page.locator('#budget-comments')).toHaveValue(/Short on FP/);

  await page.locator('tr[data-row-id="1"] input[name="usedQty"]').fill('-1');
  await page.locator('#budget-save').click();
  await expect(page.locator('#budget-message')).toContainText('used cannot be negative');
});
