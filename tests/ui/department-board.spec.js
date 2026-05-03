import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('Department Status Board Open Department opens Department Detail Editor with required controls', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();

  const board = page.locator('.department-board');
  await expect(board.getByText('PM SHIFT HANDOVER')).toBeVisible();
  await board.getByRole('button', { name: /Open Department/i }).first().click();

  const detail = page.locator('.department-detail-editor');
  await expect(detail.getByRole('heading', { name: 'DEPARTMENT DETAIL', exact: true })).toBeVisible();
  await expect(detail.getByText('Date:')).toBeVisible();
  await expect(detail.getByText('Shift:')).toBeVisible();
  await expect(detail.getByText('Session:')).toBeVisible();
  await expect(
    detail.locator('.form-label').getByText('Department status', { exact: true })
  ).toBeVisible();
  await expect(detail.locator('select').first().locator('option')).toContainText(['Completed', 'Incomplete', 'Not updated', 'Not running']);
  await expect(detail.getByText('Handover notes')).toBeVisible();
  await expect(detail.getByText('Issues / blockers')).toBeVisible();
  await expect(detail.getByText('Actions required')).toBeVisible();
  await expect(detail.getByText('Attachment summary')).toBeVisible();
  await expect(detail.getByRole('button', { name: 'Save Department' })).toBeVisible();
  await expect(detail.getByRole('button', { name: 'Reset changes' })).toBeVisible();
  await expect(detail.getByRole('button', { name: 'Open Attachments' })).toBeVisible();
  await expect(detail.getByRole('button', { name: 'Preview / Reports' })).toBeVisible();

  await detail.locator('.screen-footer')
    .getByRole('button', { name: 'Back to Department Status Board' })
    .click();
  await expect(board.getByText('DEPARTMENT STATUS')).toBeVisible();

  await board.getByRole('button', { name: 'Open Department' }).first().click();
  await page.locator('.department-detail-editor').getByRole('button', { name: 'Open Attachments' }).click();
  await expect(
    page.locator('.attachments-screen').getByRole('heading', { name: 'ATTACHMENTS', exact: true })
  ).toBeVisible();

  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();
  await page.locator('.department-board').getByRole('button', { name: 'Open Department' }).first().click();
  const previewButton = page.locator('.department-detail-editor').getByRole('button', { name: 'Preview / Reports' });
  await expect(previewButton).toBeVisible();
  await page.evaluate(() => window.__mhApp.navigate('reports'));
  await expect(page.locator('.preview-reports-screen').getByRole('heading', { name: 'PREVIEW / REPORTS', exact: true })).toBeVisible();
});

test('Direct department detail route without active department shows safe message', async ({ page }) => {
  await openMockRoute(page, 'departmentDetailEntry');
  const detail = page.locator('.department-detail-editor');
  await expect(detail.getByText('No department selected. Return to Department Status Board.')).toBeVisible();
});
