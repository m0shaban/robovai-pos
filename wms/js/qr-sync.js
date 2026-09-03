/**
 * QR Sync Bridge — جسر المزامنة بالـ QR بين المخزن والكاشير
 * يعمل بالكامل أوفلاين — كل نظام مستقل — المزامنة اختيارية
 */

import { Html5Qrcode } from 'html5-qrcode';
import QRCode from 'qrcode';

// الإصدار الحالي لبروتوكول الـ Payload
const SYNC_PROTOCOL_VERSION = 1;

// المتغير العالمي لحفظ البيانات المُستوردة
let _pendingImportData = null;
let _qrImportScanner = null;

// ──────────────────────────────────────────
//  تبديل تبويبات التصدير / الاستيراد
// ──────────────────────────────────────────
function qrSwitchTab(tab) {
  const exportSection = document.getElementById('qr-export-section');
  const importSection = document.getElementById('qr-import-section');
  const tabExportBtn = document.getElementById('qr-tab-export');
  const tabImportBtn = document.getElementById('qr-tab-import');

  if (tab === 'export') {
    exportSection.style.display = 'block';
    importSection.style.display = 'none';
    tabExportBtn.className = 'btn btn-primary';
    tabImportBtn.className = 'btn btn-outline';
    // وقف الكاميرا لو كانت شغالة
    stopQRImportScanner();
  } else {
    exportSection.style.display = 'none';
    importSection.style.display = 'block';
    tabExportBtn.className = 'btn btn-outline';
    tabImportBtn.className = 'btn btn-primary';
  }
}
window.qrSwitchTab = qrSwitchTab;

// ──────────────────────────────────────────
//  توليد QR للتصدير
// ──────────────────────────────────────────
async function generateSyncQR(bypassModal = false) {
  const exportType =
    document.getElementById('qr-export-type')?.value || 'stock';
  const displayArea = document.getElementById('qr-display-area');
  const canvas = document.getElementById('qr-canvas');
  const infoEl = document.getElementById('qr-payload-info');

  // إظهار المودال أولاً إذا كان نوع التصدير هو "فاتورة صرف" ولم نقم بالتخطي
  if (exportType === 'dispatch' && !bypassModal) {
    await openDispatchModal();
    return;
  }

  // إظهار شاشة تحميل
  if (displayArea) displayArea.style.display = 'block';
  if (infoEl) infoEl.textContent = '⏳ جاري تجهيز البيانات...';

  try {
    const payload = await buildSyncPayload(exportType);
    const jsonStr = JSON.stringify(payload);

    // تحذير حجم البيانات
    if (jsonStr.length > 2900) {
      Swal.fire({
        title: 'تنبيه',
        text: `البيانات كبيرة جداً للـ QR (${jsonStr.length} حرف). سيتم استخدام آخر 10 عناصر فقط.`,
        icon: 'warning',
        confirmButtonText: 'موافق',
      });
      payload.data = payload.data.slice(0, 10);
    }

    const finalJson = JSON.stringify(payload);

    await QRCode.toCanvas(canvas, finalJson, {
      width: 240,
      margin: 2,
      color: {
        dark: '#000000',
        light: '#ffffff',
      },
      errorCorrectionLevel: 'M',
    });

    const typeLabels = {
      stock: 'لقطة المخزون الحالي',
      inbound: 'آخر 20 حركة وارد',
      outbound: 'آخر 20 حركة صادر',
      all: 'حركات اليوم',
      supply_request: 'طلب توريد',
      dispatch: 'فاتورة صرف',
      daily_report: 'تقرير يومي',
    };

    if (infoEl) {
      infoEl.innerHTML = `
        ✅ <strong>${typeLabels[exportType]}</strong><br>
        📦 ${payload.data.length} عنصر &nbsp;|&nbsp;
        🕐 ${new Date(payload.ts * 1000).toLocaleTimeString('ar-EG')}<br>
        <span style="font-size: 0.72rem; opacity: 0.7;">اعرض هذا الـ QR أمام كاميرا نظام الكاشير</span>
      `;
    }

    // حفظ آخر QR للاستخدام لاحقاً
    window._lastQRPayload = payload;

    if (window.lucide) window.lucide.createIcons();
  } catch (err) {
    console.error('QR Generation Error:', err);
    Swal.fire('خطأ', 'فشل توليد الـ QR: ' + err.message, 'error');
    if (displayArea) displayArea.style.display = 'none';
  }
}
window.generateSyncQR = generateSyncQR;

// ──────────────────────────────────────────
//  نافذة فاتورة الصرف (Dispatch Modal)
// ──────────────────────────────────────────
window._dispatchProductsCache = []; // Caching products for search

async function openDispatchModal() {
  const modal = document.getElementById('dispatch-modal');
  const tbody = document.getElementById('dispatch-items-list');
  const searchInput = document.getElementById('dispatch-search');

  if (!modal || !tbody) return;

  // جلب المنتجات المتاحة في المخزن (رصيد > 0)
  const products = await InventoryDB.ProductService.getAll();
  window._dispatchProductsCache = products.filter((p) => (p.stock || 0) > 0);

  if (searchInput) searchInput.value = '';
  renderDispatchTable(window._dispatchProductsCache);

  modal.style.display = 'flex';
}
window.openDispatchModal = openDispatchModal;

function renderDispatchTable(productsToRender) {
  const tbody = document.getElementById('dispatch-items-list');
  if (!tbody) return;

  if (productsToRender.length === 0) {
    tbody.innerHTML =
      '<tr><td colspan="3" style="text-align:center; padding: 1rem; color: var(--text-muted);">لا توجد أصناف متاحة في المخزن</td></tr>';
    return;
  }

  tbody.innerHTML = productsToRender
    .map(
      (p) => `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05);" class="dispatch-row">
      <td style="padding: 0.5rem;">
        <div style="font-weight: 600;">${p.name}</div>
        <div style="font-size: 0.75rem; color: var(--text-muted);">${p.barcode || '-'}</div>
      </td>
      <td style="padding: 0.5rem; color: #10b981; font-weight: 600;">${p.stock}</td>
      <td style="padding: 0.5rem;">
        <input type="number"
               class="dispatch-qty-input"
               data-barcode="${p.barcode}"
               data-name="${p.name}"
               data-price="${p.price || 0}"
               data-max="${p.stock}"
               min="0" max="${p.stock}"
               value="0"
               style="width: 100%; padding: 0.25rem; text-align: center; border-radius: 4px; border: 1px solid var(--border-color); background: rgba(0,0,0,0.2); color: white;" />
      </td>
    </tr>
  `,
    )
    .join('');
}

function filterDispatchList() {
  const query = (
    document.getElementById('dispatch-search')?.value || ''
  ).toLowerCase();
  const filtered = window._dispatchProductsCache.filter(
    (p) =>
      (p.name && p.name.toLowerCase().includes(query)) ||
      (p.barcode && p.barcode.toLowerCase().includes(query)),
  );
  renderDispatchTable(filtered);
}
window.filterDispatchList = filterDispatchList;

function confirmDispatchAndGenerate() {
  const inputs = document.querySelectorAll('.dispatch-qty-input');
  const itemsToDispatch = [];

  inputs.forEach((input) => {
    const qty = parseInt(input.value || 0, 10);
    if (qty > 0) {
      const max = parseInt(input.getAttribute('data-max') || 0, 10);
      if (qty > max) {
        Swal.fire(
          'تنبيه',
          `الكمية المدخلة للصنف ${input.getAttribute('data-name')} تتجاوز الرصيد المتاح`,
          'warning',
        );
        throw new Error('كمية غير صحيحة');
      }
      itemsToDispatch.push({
        b: input.getAttribute('data-barcode'),
        n: input.getAttribute('data-name'),
        q: qty,
        pr: parseFloat(input.getAttribute('data-price')) || 0,
      });
    }
  });

  if (itemsToDispatch.length === 0) {
    Swal.fire(
      'تنبيه',
      'الرجاء إدخال كمية الصرف لصنف واحد على الأقل',
      'warning',
    );
    return;
  }

  // حفظ الأصناف المحددة وتمريرها لمولد الـ QR
  window._dispatchItems = itemsToDispatch;

  // إخفاء المودال
  document.getElementById('dispatch-modal').style.display = 'none';

  // توليد الـ QR بتخطي المودال
  generateSyncQR(true);
}
window.confirmDispatchAndGenerate = confirmDispatchAndGenerate;

// ──────────────────────────────────────────
//  بناء الـ Payload حسب النوع المطلوب
// ──────────────────────────────────────────
async function buildSyncPayload(exportType) {
  const payload = {
    v: SYNC_PROTOCOL_VERSION,
    src: 'wms', // المصدر: نظام المخزن
    type: exportType,
    ts: Math.floor(Date.now() / 1000),
    data: [],
  };

  if (exportType === 'stock') {
    const products = await InventoryDB.ProductService.getAll();
    // موحّد مع POS: b=barcode, n=name, q=stock, mn=min_stock, pr=price, c=category
    payload.data = products.map((p) => ({
      b: p.barcode || '',
      n: p.name,
      q: p.stock ?? 0,
      mn: p.min_stock ?? 0,
      pr: p.price ?? 0, // ضروري حتى أصناف جديدة تُضاف بسعر صحيح في POS
      c: p.category || '',
    }));
  } else if (exportType === 'inbound') {
    const txs = await InventoryDB.TransactionService.getAll();
    const inbound = txs
      .filter((t) => t.type === 'inbound')
      .sort(
        (a, b) =>
          new Date(b.timestamp || b.date) - new Date(a.timestamp || a.date),
      )
      .slice(0, 20);
    payload.data = inbound.map((t) => ({
      b: t.barcode || '',
      n: t.productName || t.product_name || '',
      q: t.quantity ?? 0,
      d: Math.floor(new Date(t.timestamp || t.date).getTime() / 1000),
    }));
  } else if (exportType === 'outbound') {
    const txs = await InventoryDB.TransactionService.getAll();
    const outbound = txs
      .filter((t) => t.type === 'outbound')
      .sort(
        (a, b) =>
          new Date(b.timestamp || b.date) - new Date(a.timestamp || a.date),
      )
      .slice(0, 20);
    payload.data = outbound.map((t) => ({
      b: t.barcode || '',
      n: t.productName || t.product_name || '',
      q: t.quantity ?? 0,
      d: Math.floor(new Date(t.timestamp || t.date).getTime() / 1000),
    }));
  } else if (exportType === 'supply_request') {
    // طلب توريد: الأصناف تحت الحد الأدنى + الكمية المطلوبة
    const products = await InventoryDB.ProductService.getAll();
    const lowStock = products
      .filter((p) => (p.stock ?? 0) < (p.min_stock ?? 0))
      .sort((a, b) => a.stock - a.min_stock - (b.stock - b.min_stock))
      .slice(0, 25);
    payload.data = lowStock.map((p) => ({
      b: p.barcode || '',
      n: p.name,
      q: p.stock ?? 0,
      mn: p.min_stock ?? 0,
      need: Math.max(0, (p.min_stock ?? 0) - (p.stock ?? 0)),
    }));
  } else if (exportType === 'dispatch') {
    // فاتورة صرف: نستخدم _dispatchItems اللي يتملّى من modal الإدخال
    const dispatchItems = window._dispatchItems || [];
    if (dispatchItems.length === 0) {
      throw new Error(
        'لا توجد أصناف محددة للصرف. استخدم زر "إعداد فاتورة صرف" أولاً.',
      );
    }
    payload.data = dispatchItems;
    window._dispatchItems = []; // تصفير بعد التوليد
  } else if (exportType === 'daily_report') {
    // تقرير يومي: ملخص صرف اليوم + أعلى أصناف
    // الترانزاكشنز بتخزن الأصناف في items[] و total_amount كـ top-level
    const today = new Date().toISOString().slice(0, 10);
    const txs = await InventoryDB.TransactionService.getAll();
    const todayOut = txs.filter((t) => {
      const d = new Date(t.date || t.timestamp).toISOString().slice(0, 10);
      return d === today && t.type === 'outbound';
    });

    // تجميع الأصناف من items[] بشكل صحيح
    const totals = {};
    let totalRevenue = 0;
    for (const tx of todayOut) {
      totalRevenue += tx.total_amount ?? 0;
      const itemsArr = tx.items || [];
      for (const item of itemsArr) {
        const key = item.name || item.id;
        if (!key) continue;
        if (!totals[key]) totals[key] = { b: '', n: item.name, sq: 0 };
        totals[key].sq += item.qty ?? 0;
      }
    }

    // ملخص عام — متوافق مع POS اللي بيبعت total+cash+card
    payload.data.push({
      _summary: true,
      cnt: todayOut.length,
      total: totalRevenue, // موحّد مع POS
      total_qty: Object.values(totals).reduce((s, x) => s + x.sq, 0),
    });
    // أعلى 10 أصناف مرتبة بالكمية
    Object.values(totals)
      .sort((a, b) => b.sq - a.sq)
      .slice(0, 10)
      .forEach((x) => payload.data.push(x));
  } else if (exportType === 'all') {
    // حركات اليوم فقط
    const today = new Date().toISOString().slice(0, 10);
    const txs = await InventoryDB.TransactionService.getAll();
    const todayTxs = txs.filter((t) => {
      const d = new Date(t.timestamp || t.date).toISOString().slice(0, 10);
      return d === today;
    });
    payload.data = todayTxs.map((t) => ({
      tp: t.type === 'inbound' ? 'i' : 'o',
      b: t.barcode || '',
      n: t.productName || t.product_name || '',
      q: t.quantity ?? 0,
      d: Math.floor(new Date(t.timestamp || t.date).getTime() / 1000),
    }));
  }

  return payload;
}

// ──────────────────────────────────────────
//  تحميل QR كصورة PNG
// ──────────────────────────────────────────
function downloadSyncQR() {
  const canvas = document.getElementById('qr-canvas');
  if (!canvas) return;

  const link = document.createElement('a');
  link.download = `wms-sync-${Date.now()}.png`;
  link.href = canvas.toDataURL('image/png');
  link.click();
}
window.downloadSyncQR = downloadSyncQR;

// ──────────────────────────────────────────
//  مشاركة QR (Web Share API)
// ──────────────────────────────────────────
async function shareSyncQR() {
  const canvas = document.getElementById('qr-canvas');
  if (!canvas) return;

  try {
    canvas.toBlob(async (blob) => {
      const file = new File([blob], 'wms-sync.png', { type: 'image/png' });
      if (navigator.canShare && navigator.canShare({ files: [file] })) {
        await navigator.share({ files: [file], title: 'مزامنة مخزني الذكي' });
      } else {
        // Fallback: نسخ الـ JSON للحافظة
        const json = JSON.stringify(window._lastQRPayload, null, 2);
        await navigator.clipboard.writeText(json);
        Swal.fire('تم', 'تم نسخ بيانات المزامنة للحافظة', 'success');
      }
    });
  } catch (err) {
    console.error('Share error:', err);
  }
}
window.shareSyncQR = shareSyncQR;

// ──────────────────────────────────────────
//  فتح ماسح QR للاستيراد
// ──────────────────────────────────────────
async function openQRImportScanner() {
  const readerDiv = document.getElementById('qr-import-reader');
  const resultDiv = document.getElementById('qr-import-result');
  if (!readerDiv) return;

  // إخفاء نتيجة سابقة
  if (resultDiv) resultDiv.style.display = 'none';
  readerDiv.style.display = 'block';
  readerDiv.innerHTML = '';

  _qrImportScanner = new Html5Qrcode('qr-import-reader');

  try {
    await _qrImportScanner.start(
      { facingMode: 'environment' },
      { fps: 10, qrbox: { width: 240, height: 240 } },
      (decodedText) => onQRImportSuccess(decodedText),
      () => {},
    );
  } catch (err) {
    Swal.fire('خطأ', 'تعذر فتح الكاميرا: ' + err.message, 'error');
    readerDiv.style.display = 'none';
  }
}
window.openQRImportScanner = openQRImportScanner;

function stopQRImportScanner() {
  if (_qrImportScanner) {
    _qrImportScanner.stop().catch(() => {});
    _qrImportScanner = null;
  }
  const readerDiv = document.getElementById('qr-import-reader');
  if (readerDiv) readerDiv.style.display = 'none';
}

// ──────────────────────────────────────────
//  عند نجاح مسح QR الاستيراد
// ──────────────────────────────────────────
function onQRImportSuccess(jsonText) {
  stopQRImportScanner();

  try {
    const data = JSON.parse(jsonText);

    // التحقق أن البيانات من نظام متوافق
    if (!data.v || !data.src || !data.data) {
      throw new Error('QR غير متوافق مع بروتوكول المزامنة.');
    }

    _pendingImportData = data;

    const previewEl = document.getElementById('qr-import-preview');
    const resultDiv = document.getElementById('qr-import-result');

    const srcLabel =
      data.src === 'pos' ? 'نظام الكاشير (POS)' : 'نظام المخزن (WMS)';
    const typeLabels = {
      stock: 'مخزون',
      inbound: 'وارد',
      outbound: 'صادر',
      all: 'حركات اليوم',
      supply_request: 'طلب توريد',
      dispatch: 'فاتورة صرف',
      daily_report: 'تقرير يومي',
    };
    const preview = {
      المصدر: srcLabel,
      النوع: typeLabels[data.type] || data.type,
      'عدد العناصر': data.data.length,
      'وقت التصدير': new Date(data.ts * 1000).toLocaleString('ar-EG'),
      'أول 3 عناصر': data.data.slice(0, 3),
    };

    // عرض بطاقة خاصة لطلبات التوريد
    if (data.type === 'supply_request' && previewEl) {
      const rows = data.data
        .map(
          (item) =>
            `<tr>
          <td>${item.n || '-'}</td>
          <td>${item.b || '-'}</td>
          <td style="color:#ef4444">${item.q}</td>
          <td style="color:#10b981">${item.need}</td>
        </tr>`,
        )
        .join('');
      previewEl.innerHTML = `
        <table style="width:100%;border-collapse:collapse;font-size:0.85rem">
          <thead><tr style="background:#1e3a5f;color:#fff">
            <th>الصنف</th><th>الباركود</th><th>المخزون</th><th>المطلوب</th>
          </tr></thead><tbody>${rows}</tbody>
        </table>`;
    } else if (data.type === 'daily_report' && previewEl) {
      const summary = data.data.find((x) => x._summary) || {};
      const items = data.data.filter((x) => !x._summary);
      // يدعم حقل total (من POS) أو total_qty (من WMS)
      const totalMoney =
        summary.total != null
          ? `${Number(summary.total).toFixed(2)} ج.م`
          : null;
      const totalQty =
        summary.total_qty != null ? `${summary.total_qty} وحدة` : null;
      const cashLine =
        summary.cash != null
          ? `<br>نقدي: <strong>${Number(summary.cash).toFixed(2)}</strong> | شبكة: <strong>${Number(summary.cash ?? 0).toFixed(2)}</strong>`
          : '';
      previewEl.innerHTML = `
        <div style="padding:10px;background:#f0fdf4;border-radius:8px;margin-bottom:8px;font-size:0.85rem">
          <strong>📊 ملخص اليوم</strong><br>
          عدد الحركات: <strong>${summary.cnt ?? '-'}</strong>
          ${totalMoney ? ` &nbsp;|&nbsp; الإيرادات: <strong>${totalMoney}</strong>` : ''}
          ${totalQty ? ` &nbsp;|&nbsp; إجمالي الكميات: <strong>${totalQty}</strong>` : ''}
          ${cashLine}
        </div>
        <strong>🏆 أعلى أصناف:</strong>
        <ol style="margin:4px 0 0 1.2rem;font-size:0.83rem">${items.map((x) => `<li>${x.n || '-'} — ${x.sq ?? 0} وحدة</li>`).join('')}</ol>`;
    } else {
      if (previewEl) previewEl.textContent = JSON.stringify(preview, null, 2);
    }

    if (resultDiv) resultDiv.style.display = 'block';

    if (window.lucide) window.lucide.createIcons();

    Swal.fire({
      title: '✅ تم المسح بنجاح',
      text: `استُلمت بيانات ${data.data.length} عنصر من ${srcLabel}. راجع التفاصيل وأضغط "تطبيق".`,
      icon: 'success',
      confirmButtonText: 'موافق',
    });
  } catch (err) {
    Swal.fire('خطأ في التحليل', err.message, 'error');
  }
}

// ──────────────────────────────────────────
//  تطبيق البيانات المُستوردة على المخزون
// ──────────────────────────────────────────
async function applyImportedQRData() {
  if (!_pendingImportData) {
    Swal.fire('تنبيه', 'لا توجد بيانات مُعلقة للتطبيق.', 'warning');
    return;
  }

  const { type, data } = _pendingImportData;

  const result = await Swal.fire({
    title: 'تأكيد التطبيق',
    html: `سيتم تحديث المخزون بناءً على <strong>${data.length}</strong> عنصر مُستورد.<br>لن يتم حذف أي بيانات موجودة.`,
    icon: 'question',
    showCancelButton: true,
    confirmButtonText: 'تطبيق',
    cancelButtonText: 'إلغاء',
  });

  if (!result.isConfirmed) return;

  let updated = 0,
    added = 0,
    failed = 0;

  for (const item of data) {
    try {
      if (type === 'stock') {
        // تحديث كمية صنف موجود أو إضافة صنف جديد
        const existing = await InventoryDB.ProductService.getByBarcode(item.b);
        if (existing) {
          await InventoryDB.ProductService.update(existing.id, {
            stock: item.q,
          });
          updated++;
        } else if (item.b && item.n) {
          await InventoryDB.ProductService.add({
            barcode: item.b,
            name: item.n,
            stock: item.q,
            min_stock: item.mn || 0,
            category: item.c || 'مستورد من POS',
          });
          added++;
        }
      } else if (type === 'supply_request') {
        // طلب توريد: سجّل الأصناف المطلوبة كحركات "مطلوب توريد"
        if (!item._summary && item.b) {
          await InventoryDB.TransactionService.add({
            barcode: item.b,
            productName: item.n,
            quantity: item.need || 0,
            type: 'supply_request',
            timestamp: new Date().toISOString(),
            user: 'imported-from-pos',
            notes: `طلب توريد — المخزون الحالي: ${item.q}`,
          });
          added++;
        }
      } else if (type === 'dispatch') {
        // فاتورة صرف: أضف الكميات المصروفة للمخزون في WMS (طرح)
        const existing = await InventoryDB.ProductService.getByBarcode(item.b);
        if (existing) {
          const newStock = Math.max(0, (existing.stock ?? 0) - (item.q ?? 0));
          await InventoryDB.ProductService.update(existing.id, {
            stock: newStock,
          });
          await InventoryDB.TransactionService.add({
            barcode: item.b,
            productName: item.n,
            quantity: item.q,
            type: 'outbound',
            timestamp: new Date().toISOString(),
            user: 'wms-dispatch',
            notes: 'صرف لكنتين — QR Sync',
          });
          updated++;
        }
      } else if (type === 'daily_report') {
        // تقرير يومي: احفظه فقط في IndexedDB للعرض
        if (!item._summary) {
          await InventoryDB.TransactionService.add({
            barcode: item.b || '',
            productName: item.n || '',
            quantity: item.sq || 0,
            type: 'daily_report_item',
            timestamp: new Date().toISOString(),
            user: 'imported-report',
          });
          added++;
        } else {
          // حفظ الملخص كـ metadata
          localStorage.setItem(
            `daily_report_${new Date().toISOString().slice(0, 10)}`,
            JSON.stringify(item),
          );
          added++;
        }
      } else {
        // تسجيل حركة وارد/صادر
        const txType =
          type === 'inbound'
            ? 'inbound'
            : type === 'outbound'
              ? 'outbound'
              : item.tp === 'i'
                ? 'inbound'
                : 'outbound';

        await InventoryDB.TransactionService.add({
          barcode: item.b,
          productName: item.n,
          quantity: item.q,
          type: txType,
          timestamp: item.d
            ? new Date(item.d * 1000).toISOString()
            : new Date().toISOString(),
          user: 'imported-from-pos',
        });
        added++;
      }
    } catch (err) {
      console.error('Import item error:', err, item);
      failed++;
    }
  }

  _pendingImportData = null;
  document.getElementById('qr-import-result').style.display = 'none';

  Swal.fire({
    title: '✅ اكتملت المزامنة',
    html: `
      تم تحديث <strong>${updated}</strong> صنف<br>
      تم إضافة <strong>${added}</strong> صنف/حركة جديدة<br>
      ${failed > 0 ? `⚠️ فشل <strong>${failed}</strong> عنصر` : ''}
    `,
    icon: updated + added > 0 ? 'success' : 'warning',
    confirmButtonText: 'موافق',
  });
}
window.applyImportedQRData = applyImportedQRData;

// ══════════════════════════════════════════
//  QR Authentication System
//  نظام المصادقة عبر QR — بطاقة الدخول
// ══════════════════════════════════════════

const _roleLabelsAuth = {
  super_admin: 'مدير عام',
  admin: 'مدير',
  supervisor: 'مشرف',
  worker: 'عامل',
};

/**
 * يعرض بطاقة QR لمستخدم معين (يُستدعى من قائمة المستخدمين بواسطة Admin)
 */
async function showAuthQR(username, role) {
  console.log('[QR] showAuthQR called:', username, role);
  try {
    const modal = document.getElementById('auth-qr-modal');
    const canvas = document.getElementById('auth-qr-canvas');

    if (!modal || !canvas) {
      console.error('[QR] Modal or canvas element not found', {
        modal,
        canvas,
      });
      alert('خطأ: عنصر المودال غير موجود في الصفحة');
      return;
    }

    if (!window.InventoryDB || !window.InventoryDB.AuthService) {
      console.error('[QR] InventoryDB not available', window.InventoryDB);
      alert('خطأ: قاعدة البيانات غير جاهزة');
      return;
    }

    const payload = await window.InventoryDB.AuthService.generateUserQR(
      username,
      role,
    );
    console.log('[QR] payload generated:', payload.substring(0, 40) + '...');

    await QRCode.toCanvas(canvas, payload, {
      width: 220,
      margin: 2,
      color: { dark: '#1e293b', light: '#f8fafc' },
      errorCorrectionLevel: 'M',
    });

    const nameEl = document.getElementById('auth-qr-username');
    const roleEl = document.getElementById('auth-qr-role');
    if (nameEl) nameEl.textContent = username;
    if (roleEl) roleEl.textContent = _roleLabelsAuth[role] || role;
    modal.style.display = 'flex';
    console.log('[QR] modal displayed');
  } catch (err) {
    console.error('[QR] showAuthQR error:', err);
    try {
      Swal.fire(
        'خطأ',
        'فشل توليد QR: ' + (err.message || String(err)),
        'error',
      );
    } catch (_) {
      alert('فشل توليد QR: ' + (err.message || String(err)));
    }
  }
}
window.showAuthQR = showAuthQR;

// ──────────────────────────────────────────
//  ماسح QR الدخول (على شاشة Login)
// ──────────────────────────────────────────

let _authQRScanner = null;

async function startAuthQRScan() {
  const scanModal = document.getElementById('auth-qr-scan-modal');
  const statusEl = document.getElementById('auth-qr-scan-status');
  if (!scanModal) return;

  scanModal.style.display = 'flex';
  if (statusEl) statusEl.textContent = '📷 جاري تشغيل الكاميرا...';

  // تنظيف الـ div قبل التهيئة
  const readerEl = document.getElementById('auth-qr-reader');
  if (readerEl) readerEl.innerHTML = '';

  try {
    _authQRScanner = new Html5Qrcode('auth-qr-reader');
    await _authQRScanner.start(
      { facingMode: 'environment' },
      { fps: 10, qrbox: { width: 220, height: 220 } },
      async (decodedText) => {
        await _authQRScanner.stop().catch(() => {});
        _authQRScanner = null;
        scanModal.style.display = 'none';
        await handleAuthQRLogin(decodedText);
      },
      () => {}, // ignore per-frame errors
    );
    if (statusEl) statusEl.textContent = '🔍 وجّه الكاميرا نحو QR الخاص بك';
  } catch (err) {
    scanModal.style.display = 'none';
    Swal.fire('خطأ', 'تعذّر تشغيل الكاميرا: ' + err.message, 'error');
  }
}
window.startAuthQRScan = startAuthQRScan;

function stopAuthQRScan() {
  if (_authQRScanner) {
    _authQRScanner.stop().catch(() => {});
    _authQRScanner = null;
  }
  const scanModal = document.getElementById('auth-qr-scan-modal');
  if (scanModal) scanModal.style.display = 'none';
}
window.stopAuthQRScan = stopAuthQRScan;

/**
 * يُستدعى بعد مسح QR ناجح — يتحقق ويُسجّل الدخول
 */
async function handleAuthQRLogin(qrText) {
  try {
    const userInfo = await InventoryDB.AuthService.verifyQR(qrText);

    // حفظ المستخدم في localStorage (نفس ما يفعله AuthService.login)
    localStorage.setItem(
      'robovai_user',
      JSON.stringify({ username: userInfo.username, role: userInfo.role }),
    );

    // إخفاء overlay الدخول وإظهار التطبيق
    const loginOverlay = document.getElementById('login-overlay');
    const mainApp = document.getElementById('main-app-container');
    const mainNav = document.getElementById('main-nav-bar');
    if (loginOverlay) loginOverlay.style.display = 'none';
    if (mainApp) mainApp.style.display = 'block';
    if (mainNav) mainNav.style.display = 'flex';

    // تهيئة الـ event listeners والصلاحيات
    if (typeof setupEventListeners === 'function') setupEventListeners();
    if (typeof applyPermissions === 'function') applyPermissions(userInfo.role);
    if (typeof updateCloudStatusBadge === 'function') updateCloudStatusBadge();
    if (typeof switchView === 'function') switchView('dashboard');

    Swal.fire({
      title: `مرحباً، ${userInfo.username}!`,
      text: 'تم الدخول بنجاح عبر بطاقة QR 🎉',
      icon: 'success',
      timer: 1800,
      showConfirmButton: false,
    });
  } catch (err) {
    Swal.fire({
      title: 'فشل المصادقة',
      text: err.message,
      icon: 'error',
      confirmButtonText: 'حسناً',
    });
  }
}

// ══════════════════════════════════════════════════
//  ربط جهاز POS بـ WMS  (pos-pair-v1)
// ══════════════════════════════════════════════════

const POS_PAIR_KEY = 'robovai_paired_pos';

/** إرجاع معلومات الجهاز المرتبط (أو null) */
function getPairedPos() {
  try {
    const raw = localStorage.getItem(POS_PAIR_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}
window.getPairedPos = getPairedPos;

/** تحديث شارة حالة الربط في الإعدادات */
function showPosPairingStatus() {
  const statusEl = document.getElementById('pos-pair-status');
  const badgeEl = document.getElementById('pos-pair-badge');
  const unpairBtn = document.getElementById('pos-unpair-btn');
  const info = getPairedPos();

  if (!statusEl) return;

  if (info) {
    const pairedDate = info.pairedAt
      ? new Date(info.pairedAt).toLocaleDateString('ar-EG')
      : '—';
    statusEl.innerHTML = `✅ مرتبط بكاشير: <strong>${info.deviceName || info.deviceId}</strong><br>
       <span style="font-size:12px;opacity:.7">رقم الجهاز: ${info.deviceId} — تاريخ الربط: ${pairedDate}</span>`;
    statusEl.style.color = '#15803d';
    if (badgeEl) {
      badgeEl.textContent = 'مرتبط ✅';
      badgeEl.style.background = '#15803d';
    }
    if (unpairBtn) unpairBtn.style.display = 'inline-flex';
  } else {
    statusEl.textContent = 'لا يوجد ربط بعد — امسح QR الكاشير';
    statusEl.style.color = '#6b7280';
    if (badgeEl) {
      badgeEl.textContent = 'غير مرتبط';
      badgeEl.style.background = '#6b7280';
    }
    if (unpairBtn) unpairBtn.style.display = 'none';
  }
}
window.showPosPairingStatus = showPosPairingStatus;

/** إلغاء ربط الجهاز */
function unpairPos() {
  localStorage.removeItem(POS_PAIR_KEY);
  showPosPairingStatus();
  Swal.fire({
    title: 'تم إلغاء الربط',
    icon: 'info',
    timer: 1500,
    showConfirmButton: false,
  });
}
window.unpairPos = unpairPos;

/** فتح ماسح QR ربط الجهاز */
let _pairQRScanner = null;

async function startPosPairingQRScan() {
  const modal = document.getElementById('pos-pair-scan-modal');
  const statusEl = document.getElementById('pos-pair-scan-status');
  if (!modal) return;

  modal.style.display = 'flex';
  if (statusEl) statusEl.textContent = '📷 جاري تشغيل الكاميرا...';

  const readerEl = document.getElementById('pos-pair-qr-reader');
  if (readerEl) readerEl.innerHTML = '';

  try {
    _pairQRScanner = new Html5Qrcode('pos-pair-qr-reader');
    await _pairQRScanner.start(
      { facingMode: 'environment' },
      { fps: 10, qrbox: { width: 220, height: 220 } },
      async (decodedText) => {
        await _pairQRScanner.stop().catch(() => {});
        _pairQRScanner = null;
        modal.style.display = 'none';
        handlePosPairingQR(decodedText);
      },
      () => {},
    );
    if (statusEl) statusEl.textContent = '🔍 وجّه الكاميرا نحو QR الكاشير';
  } catch (err) {
    modal.style.display = 'none';
    Swal.fire('خطأ', 'تعذّر تشغيل الكاميرا: ' + err.message, 'error');
  }
}
window.startPosPairingQRScan = startPosPairingQRScan;

function stopPosPairingQRScan() {
  if (_pairQRScanner) {
    _pairQRScanner.stop().catch(() => {});
    _pairQRScanner = null;
  }
  const modal = document.getElementById('pos-pair-scan-modal');
  if (modal) modal.style.display = 'none';
}
window.stopPosPairingQRScan = stopPosPairingQRScan;

/**
 * معالجة نص QR الربط المُمسوح — التحقق والحفظ
 */
function handlePosPairingQR(qrText) {
  try {
    let jsonStr = qrText;
    // Handle deep-link URL format: https://...?pair=<url-safe-base64>
    if (qrText.startsWith('http')) {
      const url = new URL(qrText);
      const b64 = url.searchParams.get('pair');
      if (!b64) throw new Error('QR غير صالح: لا يحتوي على معلمة pair');
      const std = b64.replace(/-/g, '+').replace(/_/g, '/');
      const padded = std + '=='.slice(0, (4 - (std.length % 4)) % 4);
      jsonStr = decodeURIComponent(escape(atob(padded)));
    }
    const data = JSON.parse(jsonStr);

    if (data.type !== 'pos-pair-v1') {
      throw new Error(
        'هذا QR ليس خاصاً بربط الكاشير. تأكد من مسح الكود الصحيح.',
      );
    }
    if (!data.deviceId || !data.deviceName) {
      throw new Error('بيانات QR غير مكتملة.');
    }

    // التحقق من انتهاء صلاحية QR (24 ساعة)
    const now = Math.floor(Date.now() / 1000);
    if (data.ts && now - data.ts > 86400) {
      throw new Error(
        'انتهت صلاحية هذا QR (أكثر من 24 ساعة). ولّد QR جديد من الكاشير.',
      );
    }

    const pairing = {
      deviceId: data.deviceId,
      deviceName: data.deviceName,
      posVersion: data.posVersion || '',
      wmsUrl: data.wmsUrl || '',
      pairedAt: new Date().toISOString(),
    };

    localStorage.setItem(POS_PAIR_KEY, JSON.stringify(pairing));
    showPosPairingStatus();

    Swal.fire({
      title: 'تم الربط بنجاح! 🎉',
      html:
        `<p>الكاشير: <strong>${data.deviceName}</strong></p>` +
        `<p style="font-size:12px;opacity:.7">رقم الجهاز: ${data.deviceId}</p>`,
      icon: 'success',
      confirmButtonText: 'رائع',
    });
  } catch (err) {
    Swal.fire({
      title: 'فشل الربط',
      text: err.message,
      icon: 'error',
      confirmButtonText: 'حسناً',
    });
  }
}
window.handlePosPairingQR = handlePosPairingQR;

// ══════════════════════════════════════════════════
//  Deep-link pairing: ?pair=<url-safe-base64>
//  Scanned by phone camera → opens WMS → auto-pairs after login
// ══════════════════════════════════════════════════
(function detectPairUrlParam() {
  try {
    const params = new URLSearchParams(window.location.search);
    const b64 = params.get('pair');
    if (!b64) return;

    // Clean URL immediately (remove ?pair=... without page reload)
    history.replaceState({}, document.title, window.location.pathname);

    // Decode URL-safe base64 → standard base64 → UTF-8 JSON
    const std = b64.replace(/-/g, '+').replace(/_/g, '/');
    const padded = std + '=='.slice(0, (4 - (std.length % 4)) % 4);
    const json = decodeURIComponent(escape(atob(padded)));

    // Store for app.js to consume after the user logs in
    window._pendingPosPairQR = json;
  } catch (e) {
    console.warn('[QR Pair] Failed to decode ?pair= param:', e);
  }
})();
