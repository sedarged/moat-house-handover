import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('Handover Session Department Board opens Department Status Board with shift/session context', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();

  const board = page.locator('.department-board');
  await expect(board.getByText('PM SHIFT HANDOVER')).toBeVisible();
  await expect(board.getByText('DEPARTMENT STATUS')).toBeVisible();
  await expect(board.getByText('SHIFT SUMMARY')).toBeVisible();

  for (const name of ['Injection', 'MetaPress', 'Berks', 'Wilts', 'Racking', 'Further Processing', 'Goods In & Despatch', 'Dry Goods', 'Additional']) {
    await expect(board.getByText(name)).toBeVisible();
  }

  for (const blocked of ['Manager', 'Supervisors', 'Admin', 'Stock Control', 'Hygiene']) {
    await expect(board.getByText(blocked)).toHaveCount(0);
  }

  await board.getByRole('button', { name: /Open Department/i }).first().click();
  const detail = page.locator('.department-detail-entry');
  await expect(detail.getByRole('heading', { name: 'Department Detail Entry' })).toBeVisible();
  await expect(detail.getByText(/Status:/)).toBeVisible();
  await detail.getByRole('button', { name: /Back to Department Status Board/i }).click();
  await expect(board.getByText('DEPARTMENT STATUS')).toBeVisible();

  await expect(board.getByRole('button', { name: 'Preview' })).toBeVisible();
  await expect(board.getByRole('button', { name: 'Budget' })).toBeVisible();

  await board.getByRole('button', { name: /Back to Handover Session/i }).click();
  await expect(page.getByText('PM Shift Handover Session')).toBeVisible();
});
