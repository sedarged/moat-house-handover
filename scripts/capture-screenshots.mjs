import { chromium } from '@playwright/test';
import { spawn } from 'node:child_process';
import { mkdir } from 'node:fs/promises';
import { join } from 'node:path';
import { installMockHostBridge, richFixture } from '../tests/ui/helpers/mockHostBridge.mjs';

const baseUrl = 'http://127.0.0.1:4173';
const outputDir = join(process.cwd(), 'test-evidence', 'screenshots');
const screenshots = [
  ['shift', '01-shift-screen.png'],
  ['dashboard', '02-dashboard.png'],
  ['budget', '05-budget.png'],
  ['preview', '06-preview.png']
];

async function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitForServer(url, timeoutMs = 15000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch {
      // keep waiting
    }
    await sleep(250);
  }
  throw new Error(`Timed out waiting for ${url}`);
}

async function openRoute(page, routeName) {
  await installMockHostBridge(page, richFixture);
  await page.goto(`${baseUrl}/webapp/index.html`, { waitUntil: 'networkidle' });
  await page.waitForFunction(() => Boolean(window.__mhApp));

  if (routeName !== 'shift') {
    await page.evaluate(({ sessionPayload, routeName }) => {
      window.__mhApp.applySessionPayload(sessionPayload);
      window.__mhApp.navigate(routeName);
    }, { sessionPayload: richFixture.sessionPayload, routeName });
  }

  await page.locator('#screen-root').waitFor({ state: 'visible' });
}

const server = spawn(process.execPath, ['scripts/serve-web-dev.mjs'], {
  stdio: ['ignore', 'pipe', 'pipe']
});

server.stdout.on('data', (chunk) => process.stdout.write(chunk));
server.stderr.on('data', (chunk) => process.stderr.write(chunk));

let browser;
try {
  await mkdir(outputDir, { recursive: true });
  await waitForServer(`${baseUrl}/webapp/index.html`);

  browser = await chromium.launch();

  for (const [routeName, filename] of screenshots) {
    const page = await browser.newPage({ viewport: { width: 1440, height: 920 } });
    await openRoute(page, routeName);
    await page.screenshot({ path: join(outputDir, filename), fullPage: true });
    await page.close();
    console.log(`Captured ${filename}`);
  }
} finally {
  if (browser) {
    await browser.close();
  }
  server.kill('SIGTERM');
}
