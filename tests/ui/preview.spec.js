import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('preview reports renders persisted session, readiness, department, budget, attachments and report actions', async ({ page }) => {
  await openMockRoute(page, 'reports');

  const preview = page.locator('.preview-reports-screen');
  await expect(preview.getByText('PREVIEW / REPORTS')).toBeVisible();
  await expect(preview.getByText(/Date:/)).toBeVisible();
  await expect(preview.getByText(/Shift:/)).toBeVisible();
  await expect(preview.getByText(/Session:/)).toBeVisible();

  const readiness = preview.locator('.preview-readiness-section');
  await expect(readiness.getByText('Department Status')).toBeVisible();
  await expect(readiness.getByText('Budget Summary')).toBeVisible();
  await expect(readiness.getByText('Attachments')).toBeVisible();
  await expect(readiness.getByText('Send readiness')).toBeVisible();
  await expect(readiness.getByText('Future phase')).toBeVisible();

  const departments = preview.locator('.preview-departments-section');
  await expect(departments.getByText('Department Status preview')).toBeVisible();
  await expect(departments.getByText('Injection')).toBeVisible();
  await expect(departments.getByText('MetaPress')).toBeVisible();
  await expect(departments.getByText('Short one operative')).toBeVisible();

  const budget = preview.locator('.preview-budget-section');
  await expect(budget.getByText('Budget Summary preview')).toBeVisible();
  await expect(budget.getByText('Total staff required')).toBeVisible();
  await expect(budget.getByText('Total staff used')).toBeVisible();
  await expect(budget.getByText('Variance')).toBeVisible();
  await expect(budget.getByText('Holiday')).toBeVisible();
  await expect(budget.getByText('Agency used')).toBeVisible();

  const attachments = preview.locator('.preview-attachments-section');
  await expect(attachments.getByText('Attachments preview')).toBeVisible();
  await expect(attachments.getByText('injector-reading.png')).toBeVisible();
  await expect(attachments.getByText('metapress-note.png')).toBeVisible();

  const actions = preview.locator('.preview-report-actions-section');
  for (const button of ['Generate Handover Report', 'Generate Budget Report', 'Generate Attachment Pack / Evidence Pack', 'Generate All Reports', 'Open Reports Folder', 'Continue to Send']) {
    await expect(actions.getByRole('button', { name: button })).toBeVisible();
  }
  await expect(actions.getByRole('button', { name: 'Continue to Send' })).toBeDisabled();
  await expect(actions.getByRole('button', { name: 'Generate Attachment Pack / Evidence Pack' })).toBeDisabled();

  await actions.getByRole('button', { name: 'Generate Handover Report' }).click();
  const output = preview.locator('.preview-output-section');
  await expect(output.getByText('handover')).toBeVisible();
  await expect(output.getByText('C:/Mock/Handover.html')).toBeVisible();
});

test('preview reports is reachable from session, department board, budget and attachments flows', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.locator('.handover-session').getByRole('button', { name: 'Preview / Reports' }).click();
  await expect(page.locator('.preview-reports-screen').getByText('PREVIEW / REPORTS')).toBeVisible();
  await page.locator('.preview-reports-screen').getByRole('button', { name: 'Back to Handover Session' }).click();
  await expect(page.locator('.handover-session').getByText('PM Shift Handover Session')).toBeVisible();

  await page.locator('.handover-session').getByRole('button', { name: 'Department Board' }).click();
  await page.locator('.department-board').getByRole('button', { name: 'Preview' }).click();
  await expect(page.locator('.preview-reports-screen').getByText('PREVIEW / REPORTS')).toBeVisible();
  await page.locator('.preview-reports-screen').getByRole('button', { name: 'Back to Department Status Board' }).click();
  await expect(page.locator('.department-board').getByText('DEPARTMENT STATUS')).toBeVisible();

  await page.locator('.department-board').getByRole('button', { name: 'Budget' }).click();
  await page.locator('.shift-budget').getByRole('button', { name: 'Print' }).click();
  await expect(page.locator('.preview-reports-screen').getByText('PREVIEW / REPORTS')).toBeVisible();
  await page.locator('.preview-reports-screen').getByRole('button', { name: 'Back to Budget' }).click();
  await expect(page.locator('.shift-budget').getByText('BUDGET SUMMARY')).toBeVisible();

  await page.locator('.app-sidebar').getByRole('button', { name: 'Attachments' }).click();
  await page.locator('.attachments-screen').getByRole('button', { name: 'Preview / Reports' }).click();
  await expect(page.locator('.preview-reports-screen').getByText('PREVIEW / REPORTS')).toBeVisible();
  await page.locator('.preview-reports-screen').getByRole('button', { name: 'Back to Attachments' }).click();
  await expect(page.locator('.attachments-screen').getByRole('heading', { name: 'ATTACHMENTS' })).toBeVisible();
});
