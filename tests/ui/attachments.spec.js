import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('attachments screen renders flow from session and board navigation', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Continue Draft/i }).click();
  const session = page.locator('.handover-session');
  await session.getByRole('button', { name: 'Attachments' }).click();

  const attachments = page.locator('.attachments-screen');
  await expect(attachments.getByRole('heading', { name: 'ATTACHMENTS' })).toBeVisible();
  await expect(attachments.getByText(/Date:/)).toBeVisible();
  await expect(attachments.getByText(/Shift:/)).toBeVisible();
  await expect(attachments.getByText(/Session:/)).toBeVisible();
  await expect(attachments.getByText('Total attachments')).toBeVisible();
  await expect(attachments.getByText('Area filter')).toBeVisible();

  const areaFilter = attachments.locator('.attachments-area-filter');
  for (const area of ['General Handover', 'Injection', 'MetaPress', 'Goods In & Despatch', 'Dry Goods', 'Additional']) {
    await expect(areaFilter.getByRole('option', { name: area })).toBeVisible();
  }

  for (const blocked of ['Brine operative', 'Rack cleaner / domestic', 'Supervisors', 'Stock controller', 'Trolley Porter T1/T2']) {
    await expect(areaFilter.getByRole('option', { name: blocked })).toHaveCount(0);
  }

  await expect(attachments.getByText('Add attachment')).toBeVisible();
  for (const button of ['Refresh', 'Add Attachment', 'Preview / Reports', 'Back to Handover Session', 'Back to Department Status Board']) {
    await expect(attachments.getByRole('button', { name: button })).toBeVisible();
  }

  await attachments.getByRole('button', { name: 'Back to Handover Session' }).click();
  await expect(page.getByText('PM Shift Handover Session')).toBeVisible();

  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();
  await page.locator('.department-board').getByRole('button', { name: 'Attachments' }).click();
  await expect(page.locator('.attachments-screen').getByRole('heading', { name: 'ATTACHMENTS' })).toBeVisible();
  await page.locator('.attachments-screen').getByRole('button', { name: 'Back to Department Status Board' }).click();
  await expect(page.locator('.department-board').getByText('DEPARTMENT STATUS')).toBeVisible();
});
