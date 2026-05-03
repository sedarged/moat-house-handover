import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('send route renders send email review screen and key sections', async ({ page }) => {
  await openMockRoute(page, 'send');
  const send = page.locator('.send-email-review-screen');
  await expect(send.getByText('SEND / EMAIL REVIEW')).toBeVisible();
  await expect(send.getByText(/Date:/)).toBeVisible();
  await expect(send.getByText(/Shift:/)).toBeVisible();
  await expect(send.getByText(/Session:/)).toBeVisible();
  await expect(send.locator('.send-recipients-section')).toContainText('Profile/source');
  await expect(send.locator('.send-subject-section')).toContainText('Email subject');
  await expect(send.locator('.send-body-section')).toContainText('Email body preview');
  await expect(send.locator('.send-output-section')).toContainText('Generated report outputs / attachments');
  await expect(send.getByRole('button', { name: 'Validate Email Package' })).toBeVisible();
  await expect(send.getByRole('button', { name: 'Create Draft / Prepare Email' })).toBeDisabled();
  await expect(send.getByRole('button', { name: 'Send Email' })).toBeDisabled();
});

test('preview continue to send opens send review and back navigation works', async ({ page }) => {
  await openMockRoute(page, 'reports');
  const preview = page.locator('.preview-reports-screen');
  await preview.getByRole('button', { name: 'Continue to Send' }).click();
  const send = page.locator('.send-email-review-screen');
  await expect(send.getByText('SEND / EMAIL REVIEW')).toBeVisible();
  await send.getByRole('button', { name: 'Back to Preview / Reports' }).click();
  await expect(preview.getByText('PREVIEW / REPORTS')).toBeVisible();
  await preview.getByRole('button', { name: 'Continue to Send' }).click();
  await send.getByRole('button', { name: 'Back to Handover Session' }).click();
  await expect(page.locator('.handover-session')).toBeVisible();
  await page.locator('.handover-session').getByRole('button', { name: 'Home' }).click();
  await expect(page.locator('.home-screen')).toBeVisible();
});
