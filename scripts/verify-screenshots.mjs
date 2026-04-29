import { readdir, stat, readFile } from 'node:fs/promises';
import { join } from 'node:path';

const screenshotDir = join(process.cwd(), 'test-evidence', 'screenshots');
const requiredFiles = [
  '01-shift-screen.png',
  '02-dashboard.png',
  '05-budget.png',
  '06-preview.png'
];

function readUInt32BE(buffer, offset) {
  return buffer.readUInt32BE(offset);
}

async function verifyPng(fileName) {
  const filePath = join(screenshotDir, fileName);
  const info = await stat(filePath);
  if (!info.isFile()) {
    throw new Error(`${fileName} is not a file`);
  }
  if (info.size < 10_000) {
    throw new Error(`${fileName} is too small (${info.size} bytes), likely blank or invalid`);
  }

  const buffer = await readFile(filePath);
  const pngSignature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  if (!buffer.subarray(0, 8).equals(pngSignature)) {
    throw new Error(`${fileName} is not a PNG file`);
  }

  const width = readUInt32BE(buffer, 16);
  const height = readUInt32BE(buffer, 20);
  if (width < 900 || height < 600) {
    throw new Error(`${fileName} dimensions too small: ${width}x${height}`);
  }

  return { fileName, size: info.size, width, height };
}

try {
  await readdir(screenshotDir);
  const results = [];
  for (const fileName of requiredFiles) {
    results.push(await verifyPng(fileName));
  }

  for (const result of results) {
    console.log(`OK ${result.fileName} ${result.width}x${result.height} ${result.size} bytes`);
  }
} catch (error) {
  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}
