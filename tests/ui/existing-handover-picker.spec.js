import { expect, test } from '@playwright/test';
import { openMockRoute, pickerFixture, richFixture } from './helpers/mockHostBridge.mjs';

test('Open Existing Handover button navigates to picker', async ({ page }) => {
  await openMockRoute(page, 'night', pickerFixture);
  await page.getByRole('button', { name: /Open Existing Handover/i }).click();
  await page.getByRole('button', { name: /Open Existing Handover Picker/i }).click();
  await expect(page.locator('.existing-handover-picker').getByRole('heading', { name: 'Open Existing Handover' })).toBeVisible();
});

test('picker displays mocked saved sessions and can open one', async ({ page }) => {
  await openMockRoute(page, 'home', pickerFixture);
  await page.evaluate(() => {
    window.__mhApp.appState.selectedShift = null;
    window.__mhApp.navigate('existingHandoverPicker');
  });
  await expect(page.locator('.picker-session-card')).toHaveCount(2);
  await page.locator('[data-session-id="2001"]').getByRole('button', { name: 'Open Session' }).click();
  await expect(page.locator('.department-board')).toBeVisible();
  await expect(page.locator('.department-board').getByText('Shift: NS')).toBeVisible();
  await expect(page.locator('.department-board').getByText('Date: 2026-05-01')).toBeVisible();
});

test('picker empty state renders', async ({ page }) => {
  await openMockRoute(page, 'existingHandoverPicker', { ...pickerFixture, sessions: [] });
  await expect(page.getByText('No saved sessions match the current filters.')).toBeVisible();
});

test('picker error state renders when list fails', async ({ page }) => {
  await openMockRoute(page, 'existingHandoverPicker', { ...pickerFixture, failSessionList: true });
  await expect(page.getByText(/Unable to load saved sessions./)).toBeVisible();
});

test('Create Today\'s Handover still works', async ({ page }) => {
  await openMockRoute(page, 'am', richFixture);
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await page.getByRole('button', { name: /Start \/ Create Session/i }).click();
  await expect(page.locator('.department-board')).toBeVisible();
  await expect(page.locator('.department-board').getByRole('heading', { name: 'PM SHIFT HANDOVER' })).toBeVisible();
});
