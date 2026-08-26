import express from 'express';
import cors from 'cors';
import { createServer } from 'http';
import { WebSocketServer, WebSocket } from 'ws';
import { fsyncSync, readFileSync, writeFileSync, existsSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const PORT = process.env.PORT || 7895;
const DATA_DIR = join(__dirname, 'data');
const SALES_FILE = join(DATA_DIR, 'cloud-sales.json');

if (!existsSync(DATA_DIR)) {
  mkdirSync(DATA_DIR, { recursive: true });
}

// ─── Data Storage Engine ──────────────────────────────────────────────────────
function loadSales() {
  if (!existsSync(SALES_FILE)) return [];
  try {
    const raw = readFileSync(SALES_FILE, 'utf-8');
    return JSON.parse(raw);
  } catch {
    return [];
  }
}

function saveSales(sales) {
  try {
    writeFileSync(SALES_FILE, JSON.stringify(sales, null, 2), 'utf-8');
  } catch (err) {
    console.error('Failed to write sales file:', err);
  }
}

let _cloudSales = loadSales();

// ─── Express & HTTP Setup ─────────────────────────────────────────────────────
const app = express();
app.use(cors());
app.use(express.json({ limit: '10mb' }));

const server = createServer(app);
const wss = new WebSocketServer({ server });

// Broadcast event to all connected WebSocket clients (Owner App)
function broadcastToOwners(event, payload) {
  const message = JSON.stringify({ event, payload, timestamp: new Date().toISOString() });
  wss.clients.forEach((client) => {
    if (client.readyState === WebSocket.OPEN) {
      client.send(message);
    }
  });
}

// WebSocket Connection Handler
wss.on('connection', (ws, req) => {
  console.log(`[WebSocket] Owner App connected from ${req.socket.remoteAddress}`);
  
  // Send initial handshake state
  ws.send(JSON.stringify({
    event: 'INIT_STATE',
    payload: {
      serverTime: new Date().toISOString(),
      activeBranches: 1,
      totalSalesToday: getTodaySalesTotal(),
      totalTransactionsToday: getTodayTransactionsCount(),
    }
  }));

  ws.on('message', (data) => {
    try {
      const msg = JSON.parse(data.toString());
      if (msg.type === 'PING') {
        ws.send(JSON.stringify({ event: 'PONG', timestamp: new Date().toISOString() }));
      }
    } catch {}
  });

  ws.on('close', () => {
    console.log('[WebSocket] Owner App disconnected');
  });
});

// ─── REST API Endpoints ───────────────────────────────────────────────────────

// 1. Health check
app.get('/api/health', (req, res) => {
  res.json({
    status: 'online',
    service: 'RobovAI Cloud Sync Server',
    version: '2.0.0',
    salesCount: _cloudSales.length,
    activeWsClients: wss.clients.size,
    serverTime: new Date().toISOString(),
  });
});

// 2. Receive sales from WPF POS (POS Cloud Sync)
app.post('/api/cloud/sync/sales', (req, res) => {
  try {
    const { branchName, sales, apiKey } = req.body;
    if (!Array.isArray(sales) || sales.length === 0) {
      return res.status(400).json({ error: 'No sales provided' });
    }

    let addedCount = 0;
    for (const sale of sales) {
      const exists = _cloudSales.some(s => s.invoiceNumber === sale.invoiceNumber);
      if (!exists) {
        const enrichedSale = {
          ...sale,
          branchName: branchName || sale.branchName || 'الفرع الرئيسي',
          syncedAt: new Date().toISOString(),
        };
        _cloudSales.unshift(enrichedSale);
        addedCount++;

        // Real-Time Alert to Owner App!
        broadcastToOwners('NEW_SALE', {
          invoiceNumber: sale.invoiceNumber,
          branchName: enrichedSale.branchName,
          totalAmount: sale.totalAmount,
          paymentMethod: sale.paymentMethod,
          cashierName: sale.cashierName || 'كاشير الفرع',
          saleDate: sale.saleDate || new Date().toISOString(),
          itemsCount: sale.itemsCount || (sale.details ? sale.details.length : 1),
        });
      }
    }

    if (addedCount > 0) {
      // Keep last 10,000 sales
      if (_cloudSales.length > 10000) _cloudSales = _cloudSales.slice(0, 10000);
      saveSales(_cloudSales);
    }

    console.log(`[CloudSync] Received ${sales.length} sales from ${branchName || 'POS'}. Added ${addedCount} new.`);
    res.json({ success: true, added: addedCount, totalSales: _cloudSales.length });
  } catch (err) {
    console.error('Error processing sales sync:', err);
    res.status(500).json({ error: err.message });
  }
});

// 3. Get Executive Dashboard Analytics (for Owner App)
app.get('/api/cloud/dashboard', (req, res) => {
  const todayStr = new Date().toISOString().split('T')[0];
  const todaySales = _cloudSales.filter(s => (s.saleDate || s.syncedAt || '').startsWith(todayStr));

  const totalRevenue = todaySales.reduce((acc, s) => acc + (Number(s.totalAmount) || 0), 0);
  const totalTransactions = todaySales.length;
  
  // Payment methods breakdown
  const paymentMethods = {
    cash: todaySales.filter(s => s.paymentMethod === 'Cash' || s.paymentMethod === 0).reduce((a, s) => a + Number(s.totalAmount || 0), 0),
    card: todaySales.filter(s => s.paymentMethod === 'Card' || s.paymentMethod === 1).reduce((a, s) => a + Number(s.totalAmount || 0), 0),
    vodafone: todaySales.filter(s => s.paymentMethod === 'VodafoneCash' || s.paymentMethod === 2).reduce((a, s) => a + Number(s.totalAmount || 0), 0),
    instapay: todaySales.filter(s => s.paymentMethod === 'InstaPay' || s.paymentMethod === 3).reduce((a, s) => a + Number(s.totalAmount || 0), 0),
  };

  // Branch breakdown
  const branchMap = {};
  for (const s of todaySales) {
    const branch = s.branchName || 'الفرع الرئيسي';
    if (!branchMap[branch]) branchMap[branch] = { name: branch, total: 0, count: 0 };
    branchMap[branch].total += Number(s.totalAmount || 0);
    branchMap[branch].count++;
  }

  res.json({
    date: todayStr,
    totalRevenue,
    totalTransactions,
    avgTicket: totalTransactions > 0 ? (totalRevenue / totalTransactions) : 0,
    paymentMethods,
    branches: Object.values(branchMap),
    recentSales: _cloudSales.slice(0, 15),
  });
});

// Helpers
function getTodaySalesTotal() {
  const todayStr = new Date().toISOString().split('T')[0];
  return _cloudSales
    .filter(s => (s.saleDate || s.syncedAt || '').startsWith(todayStr))
    .reduce((acc, s) => acc + (Number(s.totalAmount) || 0), 0);
}

function getTodayTransactionsCount() {
  const todayStr = new Date().toISOString().split('T')[0];
  return _cloudSales.filter(s => (s.saleDate || s.syncedAt || '').startsWith(todayStr)).length;
}

// ─── Start Server ─────────────────────────────────────────────────────────────
server.listen(PORT, () => {
  console.log(`
════════════════════════════════════════════════════════
 🚀 RobovAI Cloud REST & WebSocket Server v2.0
 📡 Running on: http://localhost:${PORT}
 🔌 WebSocket:  ws://localhost:${PORT}
 📂 Data file:  ${SALES_FILE}
════════════════════════════════════════════════════════
  `);
});
