import { createServer } from 'node:http';
import { createReadStream, existsSync, statSync } from 'node:fs';
import { extname, join, normalize, resolve, sep } from 'node:path';

const rootDir = resolve(process.cwd());
const port = Number(process.env.PORT || 4173);

const mimeTypes = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.mjs', 'text/javascript; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
  ['.png', 'image/png'],
  ['.jpg', 'image/jpeg'],
  ['.jpeg', 'image/jpeg'],
  ['.ico', 'image/x-icon']
]);

function safePath(urlPath) {
  const decoded = decodeURIComponent(urlPath.split('?')[0] || '/');
  const withIndex = decoded === '/' ? '/webapp/index.html' : decoded;
  const candidate = normalize(join(rootDir, withIndex));
  if (candidate !== rootDir && !candidate.startsWith(rootDir + sep)) {
    return null;
  }
  return candidate;
}

const server = createServer((req, res) => {
  const filePath = safePath(req.url || '/');
  if (!filePath || !existsSync(filePath) || !statSync(filePath).isFile()) {
    res.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
    res.end('Not found');
    return;
  }

  const contentType = mimeTypes.get(extname(filePath).toLowerCase()) || 'application/octet-stream';
  res.writeHead(200, {
    'content-type': contentType,
    'cache-control': 'no-store'
  });
  createReadStream(filePath).pipe(res);
});

server.listen(port, '127.0.0.1', () => {
  console.log(`Moat House web dev server: http://127.0.0.1:${port}/webapp/index.html`);
});

process.on('SIGTERM', () => server.close(() => process.exit(0)));
process.on('SIGINT', () => server.close(() => process.exit(0)));
