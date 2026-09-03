/**
 * RobovAI LAN Sync Engine v2.0
 * =============================================
 * نظام نقل البيانات عبر الشبكة المحلية (LAN)
 * يحل مشكلة حدود الـ QR بشكل كامل:
 *   - QR Pairing: يُستخدم فقط لإقران الجهاز بسرعة (< 2 ثانية)
 *   - LAN HTTP Streaming: نقل ملايين المنتجات عبر Wi-Fi/Ethernet بدون حدود
 *
 * الأنماط المدعومة:
 *   1. Device Discovery: اكتشاف السيرفر المحلي تلقائياً
 *   2. QR Fast Pair: مسح QR للاتصال الفوري (يحتوي IP + Token فقط)
 *   3. HTTP Push: إرسال بيانات من الـ WMS إلى السيرفر
 *   4. HTTP Pull: استلام بيانات من السيرفر إلى الـ WMS
 */

// ─── LAN Sync Configuration ─────────────────────────────────────────────────
const LAN_SYNC_CONFIG = {
  defaultPort: 7890,
  discoveryPorts: [7890, 7891, 7892, 8080, 5000],
  connectionTimeout: 5000,      // 5s timeout for connection attempts
  chunkSize: 500,               // Records per HTTP batch
  maxRetries: 3,
  retryDelay: 1000,
};

// ─── State ───────────────────────────────────────────────────────────────────
let _lanState = {
  serverUrl: null,          // e.g. "http://192.168.1.10:7890"
  sessionToken: null,       // Auth token from QR pair or manual entry
  isConnected: false,
  syncProgress: 0,
  lastError: null,
};

// ─── QR Fast Pair ─────────────────────────────────────────────────────────────

/**
 * توليد QR بيانات الإقران (Fast Pair)
 * QR يحتوي على: server URL + token فقط (< 100 حرف)
 * لا يحتوي على أي بيانات منتجات
 */
function generateFastPairQR(serverIp, port, token) {
  const payload = {
    v: 2,                          // protocol version
    type: 'lan_pair',
    url: `http://${serverIp}:${port}`,
    token: token,
    ts: Date.now(),
  };
  return JSON.stringify(payload);  // ~80 chars — QR size 1 (tiny!)
}

/**
 * معالجة بيانات QR الممسوحة
 * إذا كان QR من نوع lan_pair يتصل مباشرة بالسيرفر
 */
async function processScannedQR(qrData) {
  let parsed;
  try {
    parsed = JSON.parse(qrData);
  } catch {
    // Not a JSON QR — handle as old-style product QR
    return null;
  }

  // Fast Pair QR v2
  if (parsed.v === 2 && parsed.type === 'lan_pair') {
    return await connectToLanServer(parsed.url, parsed.token);
  }

  // Legacy QR sync (old WMS export format)
  if (parsed.v === 1 && parsed.type === 'sync') {
    return { type: 'legacy_qr', data: parsed };
  }

  return null;
}

/**
 * الاتصال بسيرفر الـ LAN
 * @param {string} serverUrl - "http://192.168.1.10:7890"
 * @param {string} token - Session token
 */
async function connectToLanServer(serverUrl, token) {
  try {
    showLanStatus('جاري الاتصال بالسيرفر...', 'info');

    const response = await fetchWithTimeout(`${serverUrl}/api/ping`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'X-Client': 'robovai-wms-pwa',
      },
    }, LAN_SYNC_CONFIG.connectionTimeout);

    if (!response.ok) throw new Error(`Server returned ${response.status}`);

    const info = await response.json();

    _lanState.serverUrl = serverUrl;
    _lanState.sessionToken = token;
    _lanState.isConnected = true;
    _lanState.lastError = null;

    saveLanConfig(serverUrl, token);
    showLanStatus(`✅ متصل بـ ${info.branchName || serverUrl}`, 'success');
    updateLanUI();

    return { success: true, serverInfo: info };
  } catch (err) {
    _lanState.isConnected = false;
    _lanState.lastError = err.message;
    showLanStatus(`❌ فشل الاتصال: ${err.message}`, 'error');
    return { success: false, error: err.message };
  }
}

// ─── LAN Discovery ────────────────────────────────────────────────────────────

/**
 * اكتشاف سيرفر RobovAI على الشبكة المحلية تلقائياً
 * يجرب نطاق الـ IP الشائع (192.168.1.x, 192.168.0.x, 10.0.0.x)
 */
async function discoverLanServer() {
  showLanStatus('🔍 جاري اكتشاف السيرفر على الشبكة...', 'info');

  // Try saved config first
  const saved = loadLanConfig();
  if (saved?.url) {
    const result = await connectToLanServer(saved.url, saved.token);
    if (result.success) return result;
  }

  // Try common local IP ranges
  const commonHosts = [
    'localhost',
    '127.0.0.1',
    '192.168.1.1', '192.168.1.100', '192.168.1.101', '192.168.1.102',
    '192.168.0.1', '192.168.0.100', '192.168.0.101',
    '10.0.0.1', '10.0.0.100', '10.0.0.101',
    '172.16.0.1', '172.16.0.100',
  ];

  for (const host of commonHosts) {
    for (const port of LAN_SYNC_CONFIG.discoveryPorts) {
      try {
        const url = `http://${host}:${port}`;
        const response = await fetchWithTimeout(`${url}/api/ping`, {
          method: 'GET',
          headers: { 'X-Client': 'robovai-discovery' },
        }, 800); // Quick 800ms probe

        if (response.ok) {
          const info = await response.json();
          if (info.service === 'robovai-pos') {
            showLanStatus(`✅ تم اكتشاف السيرفر: ${host}:${port}`, 'success');
            _lanState.serverUrl = url;
            _lanState.isConnected = true;
            return { success: true, serverUrl: url, serverInfo: info };
          }
        }
      } catch {
        // Not found at this address, continue
      }
    }
  }

  showLanStatus('⚠️ لم يُعثر على سيرفر. أدخل العنوان يدوياً أو امسح QR.', 'warning');
  return { success: false };
}

// ─── Data Transfer (Push) ─────────────────────────────────────────────────────

/**
 * إرسال جميع المنتجات إلى سيرفر الـ WPF
 * يُرسل البيانات على دفعات (chunks) لتجنب timeout الشبكة
 */
async function pushProductsToServer() {
  if (!_lanState.isConnected || !_lanState.serverUrl) {
    showLanStatus('❌ غير متصل بالسيرفر', 'error');
    return;
  }

  try {
    showLanStatus('⏳ جاري تجهيز بيانات المنتجات...', 'info');
    updateSyncProgress(0);

    // Load ALL products from IndexedDB
    const products = await InventoryDB.ProductService.getAll();
    if (!products || products.length === 0) {
      showLanStatus('⚠️ لا توجد منتجات للإرسال', 'warning');
      return;
    }

    const total = products.length;
    const chunkSize = LAN_SYNC_CONFIG.chunkSize;
    const totalChunks = Math.ceil(total / chunkSize);
    let synced = 0;
    let failed = 0;

    showLanStatus(`📦 إرسال ${total.toLocaleString()} منتج على ${totalChunks} دفعة...`, 'info');

    // Send metadata first (total count + session info)
    await fetchWithTimeout(`${_lanState.serverUrl}/api/sync/begin`, {
      method: 'POST',
      headers: buildAuthHeaders(),
      body: JSON.stringify({ totalRecords: total, entityType: 'products', chunkCount: totalChunks }),
    }, 5000);

    // Send in chunks
    for (let i = 0; i < totalChunks; i++) {
      const chunk = products.slice(i * chunkSize, (i + 1) * chunkSize);
      let retries = 0;
      let success = false;

      while (retries < LAN_SYNC_CONFIG.maxRetries && !success) {
        try {
          const response = await fetchWithTimeout(`${_lanState.serverUrl}/api/sync/products`, {
            method: 'POST',
            headers: buildAuthHeaders(),
            body: JSON.stringify({
              chunk: i,
              total: totalChunks,
              records: chunk.map(transformProductForSync),
            }),
          }, 15000); // 15s per chunk

          if (response.ok) {
            success = true;
            synced += chunk.length;
            const progress = Math.round((synced / total) * 100);
            updateSyncProgress(progress);
            showLanStatus(`📤 جاري الإرسال... ${synced.toLocaleString()} / ${total.toLocaleString()} (${progress}%)`, 'info');
          } else {
            retries++;
            await sleep(LAN_SYNC_CONFIG.retryDelay * retries);
          }
        } catch (err) {
          retries++;
          if (retries < LAN_SYNC_CONFIG.maxRetries) {
            await sleep(LAN_SYNC_CONFIG.retryDelay * retries);
          } else {
            failed += chunk.length;
          }
        }
      }
    }

    // Finalize sync
    await fetchWithTimeout(`${_lanState.serverUrl}/api/sync/commit`, {
      method: 'POST',
      headers: buildAuthHeaders(),
      body: JSON.stringify({ synced, failed }),
    }, 5000);

    if (failed === 0) {
      showLanStatus(`✅ تم إرسال ${synced.toLocaleString()} منتج بنجاح!`, 'success');
    } else {
      showLanStatus(`⚠️ تم: ${synced.toLocaleString()} ✓ | فشل: ${failed.toLocaleString()} ✗`, 'warning');
    }
    updateSyncProgress(100);

  } catch (err) {
    showLanStatus(`❌ خطأ في الإرسال: ${err.message}`, 'error');
  }
}

/**
 * استلام المنتجات من سيرفر الـ WPF
 * يستلم البيانات المُرسَلة من الكاشير إلى الـ WMS
 */
async function pullProductsFromServer() {
  if (!_lanState.isConnected || !_lanState.serverUrl) {
    showLanStatus('❌ غير متصل بالسيرفر', 'error');
    return;
  }

  try {
    showLanStatus('⏳ جاري استلام بيانات المنتجات من الكاشير...', 'info');
    updateSyncProgress(0);

    // Get total count first
    const metaResponse = await fetchWithTimeout(`${_lanState.serverUrl}/api/sync/products/meta`, {
      method: 'GET',
      headers: buildAuthHeaders(),
    }, 5000);

    if (!metaResponse.ok) throw new Error('فشل في استلام معلومات البيانات');
    const meta = await metaResponse.json();
    const { totalRecords, chunkCount } = meta;

    let received = 0;
    const allProducts = [];

    for (let i = 0; i < chunkCount; i++) {
      const response = await fetchWithTimeout(
        `${_lanState.serverUrl}/api/sync/products?chunk=${i}&size=${LAN_SYNC_CONFIG.chunkSize}`,
        { method: 'GET', headers: buildAuthHeaders() },
        15000
      );

      if (!response.ok) throw new Error(`فشل في استلام الدفعة ${i + 1}`);
      const data = await response.json();
      allProducts.push(...data.records);
      received += data.records.length;

      const progress = Math.round((received / totalRecords) * 100);
      updateSyncProgress(progress);
      showLanStatus(`📥 جاري الاستلام... ${received.toLocaleString()} / ${totalRecords.toLocaleString()} (${progress}%)`, 'info');
    }

    // Merge into IndexedDB
    showLanStatus('💾 جاري حفظ البيانات في قاعدة البيانات المحلية...', 'info');
    await mergeProductsIntoDb(allProducts);

    showLanStatus(`✅ تم استلام وحفظ ${allProducts.length.toLocaleString()} منتج!`, 'success');
    updateSyncProgress(100);

    // Refresh UI
    if (typeof renderProductsList === 'function') renderProductsList();
    if (typeof updateDashboard === 'function') updateDashboard();

  } catch (err) {
    showLanStatus(`❌ خطأ في الاستلام: ${err.message}`, 'error');
  }
}

/**
 * دمج المنتجات المستلمة مع قاعدة البيانات المحلية
 * يستخدم upsert (إدراج أو تحديث حسب الباركود)
 */
async function mergeProductsIntoDb(products) {
  const BATCH = 200;
  for (let i = 0; i < products.length; i += BATCH) {
    const batch = products.slice(i, i + BATCH);
    await InventoryDB.db.transaction('rw', InventoryDB.db.products, async () => {
      for (const p of batch) {
        const existing = await InventoryDB.db.products
          .where('barcode').equals(p.barcode).first();
        if (existing) {
          await InventoryDB.db.products.update(existing.id, {
            name: p.name,
            stock: p.stock ?? existing.stock,
            purchase_price: p.purchasePrice,
            sell_price: p.sellPrice,
            category: p.category,
            unit: p.unit,
            min_stock: p.minStock,
            last_updated: new Date().toISOString(),
          });
        } else {
          await InventoryDB.db.products.add({
            barcode: p.barcode,
            name: p.name,
            stock: p.stock ?? 0,
            purchase_price: p.purchasePrice,
            sell_price: p.sellPrice,
            category: p.category,
            unit: p.unit ?? 'قطعة',
            min_stock: p.minStock ?? 5,
            created_at: new Date().toISOString(),
            last_updated: new Date().toISOString(),
          });
        }
      }
    });
  }
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function transformProductForSync(p) {
  return {
    barcode: p.barcode,
    name: p.name,
    stock: p.stock,
    purchasePrice: p.purchase_price,
    sellPrice: p.sell_price,
    category: p.category,
    unit: p.unit,
    minStock: p.min_stock,
  };
}

function buildAuthHeaders() {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${_lanState.sessionToken || ''}`,
    'X-Client': 'robovai-wms-pwa',
    'X-Protocol': '2',
  };
}

async function fetchWithTimeout(url, options, timeoutMs) {
  const controller = new AbortController();
  const id = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, { ...options, signal: controller.signal });
  } finally {
    clearTimeout(id);
  }
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function saveLanConfig(url, token) {
  localStorage.setItem('robovai_lan_server', JSON.stringify({ url, token, savedAt: Date.now() }));
}

function loadLanConfig() {
  try {
    return JSON.parse(localStorage.getItem('robovai_lan_server') || 'null');
  } catch { return null; }
}

function showLanStatus(message, type = 'info') {
  const el = document.getElementById('lan-sync-status');
  if (!el) return;
  el.textContent = message;
  el.className = `lan-status lan-status-${type}`;
  console.log(`[LAN Sync] ${type.toUpperCase()}: ${message}`);
}

function updateSyncProgress(percent) {
  _lanState.syncProgress = percent;
  const bar = document.getElementById('lan-sync-progress-bar');
  const pct = document.getElementById('lan-sync-progress-pct');
  if (bar) bar.style.width = `${percent}%`;
  if (pct) pct.textContent = `${percent}%`;
}

function updateLanUI() {
  const statusBadge = document.getElementById('lan-connection-badge');
  const btnPush = document.getElementById('btn-lan-push');
  const btnPull = document.getElementById('btn-lan-pull');

  if (statusBadge) {
    statusBadge.textContent = _lanState.isConnected
      ? `🟢 متصل: ${_lanState.serverUrl}`
      : '🔴 غير متصل';
    statusBadge.style.color = _lanState.isConnected ? 'var(--success)' : 'var(--danger)';
  }
  if (btnPush) btnPush.disabled = !_lanState.isConnected;
  if (btnPull) btnPull.disabled = !_lanState.isConnected;
}

// ─── Manual IP Entry ──────────────────────────────────────────────────────────

async function connectManual() {
  const ipInput = document.getElementById('lan-server-ip');
  const portInput = document.getElementById('lan-server-port');
  const tokenInput = document.getElementById('lan-server-token');

  const ip = ipInput?.value?.trim();
  const port = portInput?.value?.trim() || '7890';
  const token = tokenInput?.value?.trim() || 'robovai-default';

  if (!ip) {
    showLanStatus('⚠️ أدخل عنوان IP السيرفر', 'warning');
    return;
  }

  const url = `http://${ip}:${port}`;
  await connectToLanServer(url, token);
}

// ─── QR Scanner Integration ───────────────────────────────────────────────────

/**
 * معالجة QR الممسوح في نموذج الاستيراد
 * نقطة الدخول الرئيسية من الـ QR scanner
 */
async function handleLanQRScan(qrData) {
  const result = await processScannedQR(qrData);

  if (result?.type === 'legacy_qr') {
    // Old-style QR with embedded data — import directly
    if (typeof importLegacyQRData === 'function') {
      importLegacyQRData(result.data);
    }
    return;
  }

  if (result?.success) {
    updateLanUI();
    // Auto-start pull after successful pairing
    const autoPull = document.getElementById('lan-auto-pull')?.checked;
    if (autoPull) {
      await pullProductsFromServer();
    }
  }
}

// ─── Exports ──────────────────────────────────────────────────────────────────

window.LanSync = {
  // Discovery & Connection
  discover: discoverLanServer,
  connectManual,
  connectToServer: connectToLanServer,

  // Transfer
  push: pushProductsToServer,
  pull: pullProductsFromServer,

  // QR
  generatePairQR: generateFastPairQR,
  handleQRScan: handleLanQRScan,

  // State
  getState: () => ({ ..._lanState }),
  isConnected: () => _lanState.isConnected,
};
