const http = require('http');
const fs = require('fs');
const path = require('path');

const PORT = 7890;
const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.webmanifest': 'application/manifest+json; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon'
};

function getLocalIp() {
  const os = require('os');
  const ifaces = os.networkInterfaces();
  for (const name of Object.keys(ifaces)) {
    for (const iface of ifaces[name]) {
      if (iface.family === 'IPv4' && !iface.internal) {
        return iface.address;
      }
    }
  }
  return '127.0.0.1';
}

const server = http.createServer((req, res) => {
  const urlObj = new URL(req.url, `http://${req.headers.host || 'localhost'}`);
  let reqPath = decodeURIComponent(urlObj.pathname);

  // Auto redirect /wms to /wms/
  if (reqPath.toLowerCase() === '/wms') {
    res.writeHead(302, { 'Location': '/wms/' });
    res.end();
    return;
  }

  // Handle mock /api/ping
  if (reqPath === '/api/ping') {
    res.writeHead(200, { 'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*' });
    res.end(JSON.stringify({ service: 'robovai-pos', branchName: 'المتجر الرئيسي', version: '6.0', tokenHint: 'robo***' }));
    return;
  }

  // Handle mock /api/dashboard/stats
  if (reqPath === '/api/dashboard/stats') {
    res.writeHead(200, { 'Content-Type': 'application/json', 'Access-Control-Allow-Origin': '*' });
    res.end(JSON.stringify({ todaySales: 2450.75, invoiceCount: 18, avgInvoice: 136.15, productCount: 154, lowStockCount: 4, todayExpenses: 120.00 }));
    return;
  }

  // Determine candidate root directories
  const candidateRoots = [
    path.join(__dirname, '..', 'publish', 'final-exe'),
    path.join(__dirname, '..', 'LandingPage'),
    path.join(__dirname, '..', 'smart-inventory-pro', 'dist'),
    path.join(__dirname, '..', 'src', 'SmartPOS.Infrastructure', 'Web')
  ];

  let foundFile = null;

  if (reqPath === '/' || reqPath === '/dashboard') {
    const dashboardHtml = path.join(__dirname, '..', 'src', 'SmartPOS.Infrastructure', 'Web', 'WebDashboard.html');
    if (fs.existsSync(dashboardHtml)) {
      foundFile = dashboardHtml;
    }
  } else {
    let cleanRelPath = reqPath;
    if (cleanRelPath.toLowerCase().startsWith('/wms/')) {
      cleanRelPath = cleanRelPath.substring(5);
    } else if (cleanRelPath.toLowerCase().startsWith('/wms')) {
      cleanRelPath = cleanRelPath.substring(4);
    }
    cleanRelPath = cleanRelPath.replace(/^\/+/, '');
    if (!cleanRelPath) cleanRelPath = 'index.html';

    for (const root of candidateRoots) {
      const p1 = path.join(root, 'wms', cleanRelPath);
      const p2 = path.join(root, cleanRelPath);
      if (fs.existsSync(p1) && fs.statSync(p1).isFile()) {
        foundFile = p1;
        break;
      }
      if (fs.existsSync(p2) && fs.statSync(p2).isFile()) {
        foundFile = p2;
        break;
      }
      if (!path.extname(p1) && fs.existsSync(p1 + '.html') && fs.statSync(p1 + '.html').isFile()) {
        foundFile = p1 + '.html';
        break;
      }
      if (!path.extname(p2) && fs.existsSync(p2 + '.html') && fs.statSync(p2 + '.html').isFile()) {
        foundFile = p2 + '.html';
        break;
      }
    }
  }

  if (foundFile && fs.existsSync(foundFile)) {
    const ext = path.extname(foundFile).toLowerCase();
    const contentType = MIME_TYPES[ext] || 'application/octet-stream';
    res.writeHead(200, {
      'Content-Type': contentType,
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Client, X-Protocol',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Cache-Control': 'no-cache, no-store, must-revalidate'
    });
    fs.createReadStream(foundFile).pipe(res);
  } else {
    res.writeHead(404, { 'Content-Type': 'application/json; charset=utf-8', 'Access-Control-Allow-Origin': '*' });
    res.end(JSON.stringify({ error: 'ملف WMS غير موجود', requested: reqPath }));
  }
});

server.listen(PORT, '0.0.0.0', () => {
  const ip = getLocalIp();
  console.log(`\n======================================================`);
  console.log(`  RoboVAI PRO POS & WMS Server is running!`);
  console.log(`  - Local:       http://localhost:${PORT}/wms/`);
  console.log(`  - Network IP:  http://${ip}:${PORT}/wms/`);
  console.log(`  - Dashboard:   http://localhost:${PORT}/`);
  console.log(`  - User Guide:  http://${ip}:${PORT}/wms/user-guide.html`);
  console.log(`======================================================\n`);
});
