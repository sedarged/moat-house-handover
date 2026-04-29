/**
 * Playwright screenshot runner for MOAT HOUSE HANDOVER v2 webapp.
 * Runs in browser mode (no host bridge). Captures all reachable screens.
 * Screenshots saved to test-evidence/screenshots/
 */

const { chromium } = require('/opt/node22/lib/node_modules/playwright');
const http = require('http');
const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..');
const WEBAPP_DIR = path.join(ROOT, 'webapp');
const SCREENSHOTS_DIR = path.join(__dirname, 'screenshots');
const PORT = 17843;

// Minimal static file server for ES module webapp
function createServer() {
  const mimeTypes = {
    '.html': 'text/html',
    '.js': 'application/javascript',
    '.css': 'text/css',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpeg'
  };

  return http.createServer((req, res) => {
    const urlPath = req.url === '/' ? '/index.html' : req.url;
    const filePath = path.join(WEBAPP_DIR, urlPath.split('?')[0]);
    const ext = path.extname(filePath);
    const contentType = mimeTypes[ext] || 'application/octet-stream';

    fs.readFile(filePath, (err, data) => {
      if (err) {
        res.writeHead(404);
        res.end('Not found: ' + urlPath);
        return;
      }
      res.writeHead(200, { 'Content-Type': contentType });
      res.end(data);
    });
  });
}

async function takeScreenshots() {
  const server = createServer();
  await new Promise((resolve) => server.listen(PORT, resolve));
  console.log(`Server started on http://localhost:${PORT}`);

  const browser = await chromium.launch({
    headless: true,
    executablePath: '/opt/pw-browsers/chromium-1194/chrome-linux/chrome'
  });

  const context = await browser.newContext({ viewport: { width: 1400, height: 900 } });
  const page = await context.newPage();

  const consoleErrors = [];
  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('pageerror', (err) => consoleErrors.push('PageError: ' + err.message));

  const BASE_URL = `http://localhost:${PORT}`;

  async function screenshot(filename, description, setupFn) {
    try {
      if (setupFn) await setupFn();
      await page.waitForTimeout(400);
      const screenshotPath = path.join(SCREENSHOTS_DIR, filename);
      await page.screenshot({ path: screenshotPath, fullPage: false });
      console.log(`[OK] Screenshot: ${filename} — ${description}`);
    } catch (err) {
      console.error(`[FAIL] Screenshot: ${filename} — ${err.message}`);
    }
  }

  async function clickNavButton(routeName) {
    await page.click(`[data-route="${routeName}"]`);
    await page.waitForTimeout(300);
  }

  // Load app
  await page.goto(BASE_URL, { waitUntil: 'networkidle' });
  await page.waitForTimeout(600);

  // 01 - Shift screen (default landing)
  await screenshot('01-shift-screen.png', 'Shift screen (default landing)');

  // 02 - Full page showing nav + shift
  await screenshot('02-app-with-nav.png', 'App with nav bar visible');

  // 03 - Dashboard (no session — shows empty state)
  await screenshot('03-dashboard-no-session.png', 'Dashboard screen (no session)', () => clickNavButton('dashboard'));

  // 04 - Department (no session)
  await screenshot('04-department-no-session.png', 'Department screen (no session)', () => clickNavButton('department'));

  // 05 - Budget (no session)
  await screenshot('05-budget-no-session.png', 'Budget screen (no session)', () => clickNavButton('budget'));

  // 06 - Preview (no session)
  await screenshot('06-preview-no-session.png', 'Preview screen (no session)', () => clickNavButton('preview'));

  // 07 - Image Viewer (no attachment selected)
  await screenshot('07-image-viewer-no-attachment.png', 'Image viewer (no attachment selected)', () => clickNavButton('viewer'));

  // 08 - Send screen
  await screenshot('08-send-screen.png', 'Send screen', () => clickNavButton('send'));

  // 09 - Diagnostics screen
  await screenshot('09-diagnostics-screen.png', 'Diagnostics screen (initial state)', () => clickNavButton('diagnostics'));

  // 10 - Shift screen with fields filled (validation state)
  await clickNavButton('shift');
  await page.waitForTimeout(300);
  await page.selectOption('select[name="shiftCode"]', 'PM');
  await page.fill('input[name="shiftDate"]', '2026-04-29');
  await page.fill('input[name="userName"]', 'TestUser');
  await screenshot('10-shift-filled.png', 'Shift screen with fields filled');

  // 11 - Try to submit shift form (will fail because no host bridge — shows error message)
  await page.click('button[type="submit"]');
  await page.waitForTimeout(600);
  await screenshot('11-shift-submit-error.png', 'Shift submit error (no host bridge)');

  // Back to shift to show diagnostics button
  await clickNavButton('shift');
  await page.waitForTimeout(300);
  await page.click('#open-diagnostics-btn');
  await page.waitForTimeout(400);
  await screenshot('12-diagnostics-from-shift.png', 'Diagnostics navigated from Shift');

  // Log console errors
  console.log('\n--- Browser Console Errors ---');
  if (consoleErrors.length === 0) {
    console.log('None.');
  } else {
    consoleErrors.forEach((e) => console.log('  ERROR:', e));
  }

  await browser.close();
  server.close();
  console.log('\nScreenshot runner complete.');
}

takeScreenshots().catch((err) => {
  console.error('Screenshot runner failed:', err);
  process.exit(1);
});
