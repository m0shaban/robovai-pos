/**
 * RobovAI Admin Panel JS
 * وظائف لوحة الإدارة المركزية متعددة الفروع
 * و Fast QR Pair Scanner لاقران الجهاز بالسيرفر
 */

// ─── Admin Panel State ────────────────────────────────────────────────────────
let _adminState = {
  devices: [],
  lastRefresh: null,
};

// ─── QR Fast Pair Scanner ─────────────────────────────────────────────────────

let _lanQrScanner = null;

/**
 * يفتح كاميرا لمسح QR الإقران من شاشة الكاشير
 */
function startLanPairScan() {
  const readerEl = document.getElementById('lan-qr-reader');
  if (!readerEl) return;

  readerEl.style.display = 'block';

  // Check if Html5QrcodeScanner is available (from qr-sync.js vendor)
  if (typeof Html5QrcodeScanner === 'undefined' && typeof Html5Qrcode === 'undefined') {
    readerEl.innerHTML = '<p style="color:var(--warning);padding:1rem;text-align:center;">مكتبة QR غير محملة — يرجى إضافة html5-qrcode</p>';
    return;
  }

  const ScannerClass = typeof Html5QrcodeScanner !== 'undefined'
    ? Html5QrcodeScanner
    : null;

  if (!ScannerClass) {
    // Fallback: use raw Html5Qrcode
    startLanQrScanRaw(readerEl);
    return;
  }

  _lanQrScanner = new ScannerClass('lan-qr-reader', {
    fps: 10,
    qrbox: { width: 250, height: 250 },
    rememberLastUsedCamera: true,
  }, false);

  _lanQrScanner.render(
    async (decodedText) => {
      // Stop scanner after successful scan
      _lanQrScanner.clear().catch(() => {});
      readerEl.style.display = 'none';
      showLanStatus('🔍 جاري معالجة QR...', 'info');
      await LanSync.handleQRScan(decodedText);
    },
    (error) => {
      // Ignore scan errors (just haven't found a QR yet)
    }
  );
}

function startLanQrScanRaw(readerEl) {
  const html5Qrcode = new Html5Qrcode('lan-qr-reader');
  Html5Qrcode.getCameras().then(cameras => {
    if (cameras.length === 0) {
      readerEl.innerHTML = '<p style="color:var(--danger);padding:1rem;text-align:center;">لم يتم العثور على كاميرا</p>';
      return;
    }
    html5Qrcode.start(
      { facingMode: "environment" },
      { fps: 10, qrbox: 250 },
      async (decodedText) => {
        await html5Qrcode.stop();
        readerEl.style.display = 'none';
        await LanSync.handleQRScan(decodedText);
      },
      () => {}
    );
  });
}

// ─── Admin Panel Functions ────────────────────────────────────────────────────

/**
 * تحديث لوحة الإدارة بقائمة الأجهزة من السيرفر
 */
async function refreshAdminPanel() {
  if (!LanSync || !LanSync.isConnected()) {
    const listEl = document.getElementById('admin-device-list');
    if (listEl) listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:var(--text-muted);">اتصل بالسيرفر أولاً من صفحة نقل البيانات</div>';
    return;
  }

  try {
    const state = LanSync.getState();
    const response = await fetch(`${state.serverUrl}/api/admin/devices`, {
      headers: {
        'Authorization': `Bearer ${state.sessionToken}`,
        'X-Client': 'robovai-wms-pwa',
      },
    });

    if (!response.ok) throw new Error('فشل جلب الأجهزة');
    const data = await response.json();
    _adminState.devices = data.devices || [];
    _adminState.lastRefresh = new Date();

    renderDeviceList(_adminState.devices);
    updateAdminStats(_adminState.devices);
    addAdminLog(`تم تحديث قائمة الأجهزة — ${_adminState.devices.length} جهاز`);
  } catch (err) {
    addAdminLog(`خطأ: ${err.message}`);
    // Show mock data for demo when server is not available
    renderDeviceListDemo();
  }
}

function renderDeviceList(devices) {
  const listEl = document.getElementById('admin-device-list');
  if (!listEl) return;

  if (!devices || devices.length === 0) {
    listEl.innerHTML = '<div style="text-align:center;padding:2rem;color:var(--text-muted);">لا توجد أجهزة متصلة حالياً</div>';
    return;
  }

  listEl.innerHTML = devices.map(device => `
    <div class="list-item" style="border-radius:0.75rem;background:rgba(255,255,255,0.03);margin-bottom:0.5rem;border-bottom:none;padding:0.75rem 1rem;">
      <div>
        <div style="font-weight:600;font-size:0.9rem;">${escapeHtml(device.name || 'جهاز غير معروف')}</div>
        <div style="font-size:0.78rem;color:var(--text-muted);">${escapeHtml(device.ipAddress || '')} · ${escapeHtml(device.type || 'POS')}</div>
        <div style="font-size:0.75rem;color:var(--text-muted);">آخر نشاط: ${formatRelativeTime(device.lastSeen)}</div>
      </div>
      <div style="text-align:left;">
        <span style="
          display:inline-block; padding:0.2rem 0.6rem; border-radius:99px;
          font-size:0.75rem; font-weight:600;
          background:${device.online ? 'rgba(16,185,129,0.15)' : 'rgba(239,68,68,0.1)'};
          color:${device.online ? '#10b981' : '#ef4444'};
        ">${device.online ? '● متصل' : '○ غير متصل'}</span>
        <div style="font-size:0.78rem;color:var(--text-muted);margin-top:0.25rem;">${(device.totalSales || 0).toLocaleString()} مبيعات اليوم</div>
      </div>
    </div>
  `).join('');
}

function renderDeviceListDemo() {
  // Demo data for when server is unreachable
  const demoDevices = [
    { name: 'كاشير الفرع الرئيسي', ipAddress: '192.168.1.10', type: 'POS', online: true, lastSeen: new Date(), totalSales: 15420 },
    { name: 'كاشير الفرع 2', ipAddress: '192.168.1.11', type: 'POS', online: true, lastSeen: new Date(Date.now() - 120000), totalSales: 8730 },
    { name: 'كاشير مخزن المطار', ipAddress: '192.168.2.5', type: 'POS', online: false, lastSeen: new Date(Date.now() - 3600000), totalSales: 0 },
  ];
  renderDeviceList(demoDevices);
  updateAdminStats(demoDevices);

  const listEl = document.getElementById('admin-device-list');
  if (listEl) {
    const note = document.createElement('p');
    note.style.cssText = 'font-size:0.78rem;color:var(--text-muted);text-align:center;margin-top:0.5rem;';
    note.textContent = '* بيانات تجريبية — اتصل بالسيرفر لعرض البيانات الحقيقية';
    listEl.appendChild(note);
  }
}

function updateAdminStats(devices) {
  const total = devices.length;
  const active = devices.filter(d => d.online).length;

  const totalEl = document.getElementById('admin-total-branches');
  const activeEl = document.getElementById('admin-active-branches');
  if (totalEl) totalEl.textContent = total;
  if (activeEl) activeEl.textContent = active;
}

/**
 * إرسال تحديث المنتجات لجميع الفروع المتصلة دفعة واحدة
 */
async function adminBroadcastSync() {
  if (!LanSync || !LanSync.isConnected()) {
    alert('يجب الاتصال بالسيرفر أولاً');
    return;
  }

  const confirmed = confirm(`هل تريد إرسال آخر تحديثات المنتجات لجميع الفروع المتصلة (${_adminState.devices.filter(d => d.online).length} فروع)؟`);
  if (!confirmed) return;

  addAdminLog('بدء إرسال التحديثات لجميع الفروع...');
  await LanSync.push();
  addAdminLog('✅ تم إرسال التحديثات بنجاح');
}

/**
 * تصدير تقرير شامل لجميع الفروع
 */
async function adminExportReport() {
  if (!LanSync || !LanSync.isConnected()) {
    alert('يجب الاتصال بالسيرفر أولاً لجلب بيانات الفروع');
    return;
  }

  try {
    const state = LanSync.getState();
    const response = await fetch(`${state.serverUrl}/api/admin/report`, {
      headers: { 'Authorization': `Bearer ${state.sessionToken}` },
    });

    if (!response.ok) throw new Error('فشل جلب التقرير');
    const data = await response.json();

    // Build CSV
    const rows = [['الفرع', 'المبيعات اليوم', 'المنتجات', 'آخر مزامنة']];
    (data.branches || []).forEach(b => {
      rows.push([b.name, b.todaySales, b.productCount, b.lastSync]);
    });

    const csv = rows.map(r => r.join(',')).join('\n');
    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `robovai-admin-report-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
    addAdminLog('✅ تم تصدير التقرير');
  } catch (err) {
    addAdminLog(`❌ خطأ في التصدير: ${err.message}`);
    alert(`خطأ: ${err.message}`);
  }
}

function addAdminLog(message) {
  const logEl = document.getElementById('admin-sync-log');
  if (!logEl) return;

  const timestamp = new Date().toLocaleTimeString('ar-EG');
  const entry = document.createElement('div');
  entry.style.cssText = 'direction:rtl; margin-bottom:0.25rem;';
  entry.textContent = `[${timestamp}] ${message}`;
  logEl.insertBefore(entry, logEl.firstChild);

  // Keep max 50 entries
  while (logEl.children.length > 50) {
    logEl.removeChild(logEl.lastChild);
  }
}

// ─── Utils ─────────────────────────────────────────────────────────────────────

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function formatRelativeTime(date) {
  if (!date) return 'غير معروف';
  const d = date instanceof Date ? date : new Date(date);
  const diff = Math.floor((Date.now() - d.getTime()) / 1000);
  if (diff < 60) return 'منذ لحظات';
  if (diff < 3600) return `منذ ${Math.floor(diff/60)} دقيقة`;
  if (diff < 86400) return `منذ ${Math.floor(diff/3600)} ساعة`;
  return `منذ ${Math.floor(diff/86400)} يوم`;
}

// ─── Initialize ───────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
  // Auto-refresh admin panel when section becomes active
  const observer = new MutationObserver(() => {
    const adminSection = document.getElementById('admin-panel');
    if (adminSection && adminSection.classList.contains('active')) {
      if (!_adminState.lastRefresh || (Date.now() - _adminState.lastRefresh) > 30000) {
        refreshAdminPanel();
      }
    }
  });

  const container = document.querySelector('.container');
  if (container) observer.observe(container, { subtree: true, attributeFilter: ['class'] });
});
