import http from 'http';
import fs from 'fs';
import path from 'path';
import {fileURLToPath} from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const distDir = path.join(__dirname, '..', 'dist', 'finance-sentry', 'browser');
const PORT = process.env['PORT'] ?? 4200;

const MIME = {
  '.js': 'application/javascript',
  '.mjs': 'application/javascript',
  '.css': 'text/css',
  '.html': 'text/html',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.txt': 'text/plain',
};

const server = http.createServer((req, res) => {
  let reqPath = req.url.split('?')[0];
  if (reqPath === '/') reqPath = '/index.html';

  const fullPath = path.join(distDir, reqPath);

  // SPA fallback: serve index.html for non-asset routes
  const ext = path.extname(reqPath);
  const isAsset = ext && ext !== '.html';
  const filePath = isAsset && fs.existsSync(fullPath) ? fullPath : path.join(distDir, 'index.html');

  const mime = MIME[path.extname(filePath)] ?? 'application/octet-stream';
  try {
    const data = fs.readFileSync(filePath);
    res.writeHead(200, {'Content-Type': mime});
    res.end(data);
  } catch {
    res.writeHead(404);
    res.end('Not found');
  }
});

server.listen(PORT, () => {
  // Signal readiness
  process.stdout.write(`Server listening on http://localhost:${PORT}\n`);
});
