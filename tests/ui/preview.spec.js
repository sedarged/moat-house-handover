import { expect, test } from '@playwright/test';
import { openMockRoute } from './helpers/mockHostBridge.mjs';

test('preview renders persisted session, department and budget data through mock host bridge', async ({ page }) => {
  await openMockRoute(page, 'preview');

  const screenRoot = page.locator('#screen-root');
  await expect(screenRoot).toContainText('HANDOVER PREVIEW');
  await expect(screenRoot).toContainText('Injection');
  await expect(screenRoot).toContainText('MetaPress');
  await expect(screenRoot).toContainText('Budget');
  await expect(screenRoot).toContainText('Short one operative');
});
