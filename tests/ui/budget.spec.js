import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('budget summary renders shift labour budget table and validates negative staff values', async ({ page }) => {
  await openMockRoute(page, 'budgetMenu');

  const budget = page.locator('.shift-budget');
  await expect(budget.getByText('BUDGET SUMMARY')).toBeVisible();
  await expect(budget.getByText('Date:')).toBeVisible();
  await expect(budget.getByText('Shift: PM')).toBeVisible();
  await expect(budget.getByText('Lines planned:')).toBeVisible();
  await expect(budget.getByText('Last updated:')).toBeVisible();

  for (const heading of ['Department', 'Budget Staff', 'Staff Used', 'Reason']) {
    await expect(budget.getByRole('columnheader', { name: heading })).toBeVisible();
  }

  for (const name of ['Injection', 'MetaPress', 'Berks', 'Wilts', 'Further Processing', 'Goods In', 'Dry Goods', 'Supervisors', 'Admin', 'Cleaners', 'Stock controller', 'Training', 'Trolley Porter T1/T2', 'Butchery']) {
    await expect(budget.getByText(name)).toBeVisible();
  }

  await expect(budget.locator('.section-header').getByText('Summary', { exact: true })).toBeVisible();
  await expect(budget.getByText('Total staff required')).toBeVisible();
  await expect(budget.getByText('Total number of staff used')).toBeVisible();
  await expect(budget.getByText('Variance')).toBeVisible();
  await expect(budget.locator('.section-header').getByText('Comments', { exact: true })).toBeVisible();

  for (const action of ['Refresh', 'Edit', 'Save & Close', 'Print']) {
    await expect(budget.getByRole('button', { name: action })).toBeVisible();
  }

  await budget.getByRole('button', { name: 'Edit' }).click();
  await budget.locator('tr[data-row-id="1"] input[name="usedQty"]').fill('-1');
  await budget.getByRole('button', { name: 'Save & Close' }).click();
  await expect(budget.locator('#budget-message')).toContainText('used cannot be negative');
});

test('budget summary opens from handover session and department status board', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Budget' }).click();
  await expect(page.locator('.shift-budget').getByText('BUDGET SUMMARY')).toBeVisible();

  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();
  await page.locator('.department-board').getByRole('button', { name: 'Budget' }).click();
  await expect(page.locator('.shift-budget').getByText('BUDGET SUMMARY')).toBeVisible();
});
