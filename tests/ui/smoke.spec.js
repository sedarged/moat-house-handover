import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('home screen loads in browser with mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'home');
  await expect(page.locator('#screen-root')).toBeVisible();
  await expect(page.getByText(/MOAT HOUSE HANDOVER/i)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Open' }).first()).toBeVisible();
});

test('AM dashboard renders shift header, hours, and key action cards', async ({ page }) => {
  await openMockRoute(page, 'am');
  await expect(page.getByText('AM Shift Handover')).toBeVisible();
  await expect(page.getByText('Hours: 06:00 – 14:00')).toBeVisible();
  await expect(page.getByRole('button', { name: /Create Today's Handover/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /Preview \/ Reports/i })).toBeVisible();
});

test('PM dashboard renders shift header, hours, and key action cards', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await expect(page.getByText('PM Shift Handover')).toBeVisible();
  await expect(page.getByText('Hours: 14:00 – 22:00')).toBeVisible();
  await expect(page.getByRole('button', { name: /Continue Draft/i })).toBeVisible();
  await expect(page.locator('.shift-dashboard').getByRole('button', { name: /Budget/i })).toBeVisible();
});

test('Night dashboard renders shift header, hours, and key action cards', async ({ page }) => {
  await openMockRoute(page, 'night');
  await expect(page.getByText('Night Shift Handover')).toBeVisible();
  await expect(page.getByText('Hours: 22:00 – 06:00')).toBeVisible();
  await expect(page.getByRole('button', { name: /Open Existing Handover/i })).toBeVisible();
  await expect(page.getByRole('button', { name: /Back to Home/i })).toBeVisible();
});


test('AM dashboard Create opens AM session create screen', async ({ page }) => {
  await openMockRoute(page, 'am');
  await page.getByRole('button', { name: /Create Today's Handover/i }).click();
  await expect(page.getByText('AM Shift Handover Session')).toBeVisible();
  await expect(page.getByText(/Mode: Create/)).toBeVisible();
});

test('PM dashboard Continue opens PM session continue screen', async ({ page }) => {
  await openMockRoute(page, 'pm');
  await page.getByRole('button', { name: /Continue Draft/i }).click();
  await expect(page.getByText('PM Shift Handover Session')).toBeVisible();
  await expect(page.getByText(/Mode: Continue/)).toBeVisible();
});

test('Night session open supports back navigation and workflow cards visible', async ({ page }) => {
  await openMockRoute(page, 'night');
  await page.getByRole('button', { name: /Open Existing Handover/i }).click();
  await expect(page.getByText('Night Shift Handover Session')).toBeVisible();
  const session = page.locator('.handover-session');
  await expect(session.getByRole('button', { name: 'Department Board' })).toBeVisible();
  await expect(session.getByRole('button', { name: 'Budget' })).toBeVisible();
  await expect(session.getByRole('button', { name: 'Attachments' })).toBeVisible();
  await expect(session.getByRole('button', { name: 'Preview / Reports' })).toBeVisible();
  await page.getByRole('button', { name: /Back to Shift Dashboard/i }).click();
  await expect(page.getByText('Night Shift Handover')).toBeVisible();
});

test('admin route renders diagnostics cards and actions', async ({ page }) => {
  await openMockRoute(page, 'admin');
  const admin = page.locator('.admin-diagnostics-screen');
  await expect(admin.getByRole('heading', { name: 'ADMIN / DIAGNOSTICS' })).toBeVisible();
  await expect(admin.getByText('Runtime health')).toBeVisible();
  await expect(admin.getByText('Host bridge')).toBeVisible();
  await expect(admin.getByText('Effective provider')).toBeVisible();
  await expect(admin.getByText('Data root')).toBeVisible();
  await expect(admin.getByText('App lock')).toBeVisible();
  await expect(admin.getByText('Reports folder')).toBeVisible();
  await expect(admin.getByText('Attachments folder')).toBeVisible();
  await expect(admin.getByText('Backup readiness')).toBeVisible();
  await expect(admin.getByText('Email / send readiness')).toBeVisible();
  await expect(admin.getByRole('button', { name: 'Refresh Diagnostics' })).toBeVisible();
  await expect(admin.getByRole('button', { name: 'Check Runtime Status' })).toBeVisible();
  await expect(admin.getByRole('button', { name: 'Check Data Root' })).toBeVisible();
  await expect(admin.getByRole('button', { name: 'Open Settings' })).toBeVisible();
});

test('settings route renders settings and supports navigation', async ({ page }) => {
  await openMockRoute(page, 'settings');
  const settings = page.locator('.settings-screen');
  await expect(settings.getByRole('heading', { name: 'SETTINGS' })).toBeVisible();
  await expect(settings.getByText('Provider mode')).toBeVisible();
  await expect(settings.getByText('App data root')).toBeVisible();
  await expect(settings.getByText('Reports path')).toBeVisible();
  await expect(settings.getByText('Attachments path')).toBeVisible();
  await expect(settings.getByText('Email profile readiness')).toBeVisible();
  await settings.getByRole('button', { name: 'Back to Admin / Diagnostics' }).click();
  await expect(page.locator('.admin-diagnostics-screen').getByRole('heading', { name: 'ADMIN / DIAGNOSTICS' })).toBeVisible();
  await page.locator('.admin-diagnostics-screen').getByRole('button', { name: 'Home' }).click();
  await expect(page.locator('.mh-home-wrap')).toBeVisible();
});
