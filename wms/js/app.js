/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

// Application State
const state = {
  currentView: 'dashboard',
  activeOutboundItems: [],
  scannerMode: null, // 'product-search', 'inbound', 'outbound', 'new-product'
  charts: {
    txChart: null,
    catChart: null,
  },
  // لحماية المدخلات من الماسح السريع (Scanner Throttling/Debouncing)
  lastScanTime: 0,
  lastScannedCode: '',
};

/**
 * Initialize App
 */
async function initApp() {
  // Register Service Worker for PWA
  if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
      navigator.serviceWorker
        .register('./sw.js')
        .then((reg) => console.log('SW Registered'))
        .catch((err) => console.log('SW Registration Failed', err));
    });
  }

  const loginForm = document.getElementById('login-form');
  if (loginForm) {
    loginForm.addEventListener('submit', handleLoginSubmit);
  }

  // Suppliers Form
  const supForm = document.getElementById('supplier-form');
  if (supForm) {
    supForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const name = document.getElementById('sup-name').value.trim();
      const phone = document.getElementById('sup-phone').value.trim();
      if (!name || !phone) return;
      await InventoryDB.SupplierService.add({ name, phone });
      supForm.reset();
      renderSuppliersList();
      Swal.fire('تم', 'تم إضافة المورد بنجاح', 'success');
    });
  }

  // Branches Form
  const branchForm = document.getElementById('branch-form');
  if (branchForm) {
    branchForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const name = document.getElementById('branch-name').value.trim();
      if (!name) return;
      await InventoryDB.BranchService.add({ name });
      branchForm.reset();
      renderBranchesList();
      Swal.fire('تم', 'تم إضافة الفرع بنجاح', 'success');
    });
  }

  // Damages Form
  const damageForm = document.getElementById('damage-form');
  if (damageForm) {
    damageForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const barcode = document.getElementById('dmg-barcode').value.trim();
      const qty = parseInt(document.getElementById('dmg-qty').value);
      const reason = document.getElementById('dmg-reason').value;

      try {
        const product = await InventoryDB.ProductService.getByBarcode(barcode);
        if (!product) throw new Error('المنتج غير موجود');
        if (product.stock < qty)
          throw new Error('الرصيد أقل من الكمية التالفة');

        await InventoryDB.db.products.update(product.id, {
          stock: product.stock - qty,
          last_updated: new Date().toISOString(),
        });

        await InventoryDB.DamageService.add({
          barcode,
          productName: product.name,
          quantity: qty,
          reason,
          date: new Date().toISOString(),
        });

        damageForm.reset();
        renderDamagesList();
        updateDashboard();
        Swal.fire('تم', 'تم خصم التالف من الرصيد بنجاح', 'success');
      } catch (err) {
        Swal.fire('خطأ', err.message, 'error');
      }
    });
  }

  // Kitting Form
  const kitForm = document.getElementById('kit-form');
  if (kitForm) {
    kitForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const kitName = document.getElementById('kit-name').value.trim();
      const kitBarcode = document.getElementById('kit-barcode').value.trim();
      const kitPrice = document.getElementById('kit-price').value;

      if (kitComponents.length === 0) {
        return Swal.fire('خطأ', 'الرجاء إضافة مكونات للعرض', 'error');
      }

      try {
        await window.InventoryDB.db.transaction(
          'rw',
          window.InventoryDB.db.products,
          window.InventoryDB.db.kits,
          async () => {
            // 1. Deduct components
            for (let comp of kitComponents) {
              const product = await window.InventoryDB.db.products.get(comp.id);
              if (!product || product.stock < comp.qty) {
                throw new Error(`الكمية المتاحة من ${comp.name} غير كافية`);
              }
              await window.InventoryDB.db.products.update(comp.id, {
                stock: product.stock - comp.qty,
              });
            }

            // 2. Save Kit Definition
            await window.InventoryDB.db.kits.add({
              name: kitName,
              barcode: kitBarcode,
              components: kitComponents,
              price: Number(kitPrice),
            });

            // 3. Add kit as a new product in the store
            const existingKit = await window.InventoryDB.db.products
              .where('barcode')
              .equals(kitBarcode)
              .first();
            if (existingKit) {
              await window.InventoryDB.db.products.update(existingKit.id, {
                stock: existingKit.stock + 1,
              });
            } else {
              await window.InventoryDB.ProductService.add({
                barcode: kitBarcode,
                name: kitName + ' (عرض)',
                category: 'عروض وباقات',
                price: Number(kitPrice),
                stock: 1,
                min_stock: 0,
              });
            }
          },
        );

        Swal.fire('تم بنجاح', 'تم تجميع العرض وإضافته للمخزون', 'success');
        kitComponents = [];
        renderKitComponentsList();
        kitForm.reset();
        updateDashboard();
      } catch (err) {
        Swal.fire('خطأ', err.message, 'error');
      }
    });
  }

  checkAuth();

  try {
    await InventoryDB.AuthService.initialize();
  } catch (err) {
    console.error('Auth Init Error', err);
  }

  // Enable login button now that DB is ready
  const loginBtn = document.getElementById('login-submit-btn');
  if (loginBtn) {
    loginBtn.disabled = false;
    const btnText = document.getElementById('login-btn-text');
    if (btnText) btnText.textContent = 'دخول';
  }

  const currentUser = InventoryDB.AuthService.getCurrentUser();
  if (currentUser) {
    document.getElementById('login-overlay').style.display = 'none';
    document.getElementById('main-app-container').style.display = 'block';
    document.getElementById('main-nav-bar').style.display = 'flex';

    setupEventListeners();
    applyPermissions(currentUser.role);

    // Show cloud setup if Firebase is configured but account not yet registered
    if (
      InventoryDB.FirebaseService?.isConfigured &&
      !InventoryDB.FirebaseService?.isAccountSetup &&
      ['admin', 'super_admin'].includes(currentUser.role)
    ) {
      const overlay = document.getElementById('cloud-setup-overlay');
      if (overlay) overlay.style.display = 'flex';
    }

    updateCloudStatusBadge();
    if (typeof showPosPairingStatus === 'function') showPosPairingStatus();
  }
}

/**
 * Update the #connection-status badge based on Firebase state
 */
function updateCloudStatusBadge() {
  const badge = document.getElementById('connection-status');
  if (!badge) return;
  const fs = InventoryDB.FirebaseService;
  if (fs?.isConfigured && fs?.isAccountSetup) {
    badge.textContent = 'سحابي ☁️';
    badge.className = 'badge badge-primary';
  } else {
    badge.textContent = 'يعمل محلياً';
    badge.className = 'badge badge-success';
  }
}

/**
 * checkAuth — quick pre-DB check to ensure login overlay is visible
 */
function checkAuth() {
  const user = InventoryDB.AuthService.getCurrentUser();
  if (!user) {
    const overlay = document.getElementById('login-overlay');
    if (overlay) overlay.style.display = 'flex';
  }
}

/**
 * Login form submit handler
 */
async function handleLoginSubmit(e) {
  e.preventDefault();
  const username = document.getElementById('login-username').value.trim();
  const password = document.getElementById('login-password').value;
  const btn = document.getElementById('login-submit-btn');
  const btnText = document.getElementById('login-btn-text');

  if (!username || !password) return;

  if (btn) btn.disabled = true;
  if (btnText) btnText.textContent = 'جارٍ الدخول...';

  try {
    const user = await InventoryDB.AuthService.login(username, password);

    document.getElementById('login-overlay').style.display = 'none';
    document.getElementById('main-app-container').style.display = 'block';
    document.getElementById('main-nav-bar').style.display = 'flex';

    setupEventListeners();
    applyPermissions(user.role);

    // Show cloud setup overlay if Firebase configured but account not yet registered
    if (
      InventoryDB.FirebaseService?.isConfigured &&
      !InventoryDB.FirebaseService?.isAccountSetup &&
      ['admin', 'super_admin'].includes(user.role)
    ) {
      const overlay = document.getElementById('cloud-setup-overlay');
      if (overlay) overlay.style.display = 'flex';
    }

    // Process any pending QR pairing injected via ?pair= URL parameter
    if (window._pendingPosPairQR) {
      const pendingQR = window._pendingPosPairQR;
      window._pendingPosPairQR = null;
      // Small delay so the main UI is fully shown first
      setTimeout(() => {
        if (typeof window.handlePosPairingQR === 'function') {
          window.handlePosPairingQR(pendingQR);
        }
      }, 500);
    }

    updateCloudStatusBadge();
    switchView('dashboard');
  } catch (err) {
    Swal.fire('خطأ في الدخول', err.message, 'error');
    if (btn) btn.disabled = false;
    if (btnText) btnText.textContent = 'دخول';
  }
}

/**
 * Global navigation
 */
function switchView(viewId) {
  const user = InventoryDB.AuthService.getCurrentUser();
  if (!user) {
    // Session expired (e.g. iOS PWA backgrounded) — redirect to login
    document.getElementById('login-overlay').style.display = 'flex';
    document.getElementById('main-app-container').style.display = 'none';
    document.getElementById('main-nav-bar').style.display = 'none';
    return;
  }

  // RBAC Security Guard
  if (user.role === 'worker') {
    const allowed = ['products', 'outbound'];
    if (!allowed.includes(viewId)) {
      Swal.fire('تم الرفض', 'غير مصرح لك بالدخول لهذه الشاشة.', 'error');
      return;
    }
  } else if (user.role === 'supervisor') {
    const blocked = ['settings'];
    if (blocked.includes(viewId)) {
      Swal.fire('تم الرفض', 'غير مصرح لك بالدخول للإعدادات.', 'error');
      return;
    }
  }

  document
    .querySelectorAll('section')
    .forEach((s) => s.classList.remove('active'));
  document
    .querySelectorAll('.nav-item')
    .forEach((n) => n.classList.remove('active'));

  const activeSection = document.getElementById(viewId);
  const activeNavItem = document.querySelector(
    `.nav-item[data-view="${viewId}"]`,
  );

  if (activeSection) activeSection.classList.add('active');
  if (activeNavItem) activeNavItem.classList.add('active');

  state.currentView = viewId;

  // View specific logic
  if (viewId === 'dashboard')
    updateDashboard().catch((err) =>
      console.error('Dashboard render error:', err),
    );
  if (viewId === 'products') (populateCategoriesFilter(), renderProductsList());
  if (viewId === 'suppliers') renderSuppliersList();
  if (viewId === 'branches') renderBranchesList();
  if (viewId === 'damages') renderDamagesList();
  if (viewId === 'kitting') renderKittingView();
  if (viewId === 'audit-logs' && ['admin', 'super_admin'].includes(user.role))
    renderAuditLogs();
  if (viewId === 'outbound') setupOutboundView();
  if (viewId === 'stocktake') initializeStocktakeView();
  if (viewId === 'transactions') renderTransactionHistory();
  if (viewId === 'settings' && ['admin', 'super_admin'].includes(user.role))
    renderUsersList();

  // Reset scanner if changing pages
  ScannerManager.stop();
}
window.switchView = switchView;

/**
 * Dashboard Logic
 */
async function updateDashboard() {
  const products = await InventoryDB.ProductService.getAll();
  const transactions = await InventoryDB.TransactionService.getAll();
  const recentTx = await InventoryDB.TransactionService.getRecent(5);

  const totalItems = products.length;
  const lowStock = products.filter((p) => p.stock <= p.min_stock).length;

  let totalInventoryValue = 0;
  let totalExpectedRevenue = 0;

  products.forEach((p) => {
    // Fallback to 0 if NaN or undefined
    const purchasePrice = parseFloat(p.purchase_price) || 0;
    const sellPrice = parseFloat(p.price) || 0;
    const qty = parseInt(p.stock) || 0;

    totalInventoryValue += purchasePrice * qty;
    totalExpectedRevenue += sellPrice * qty;
  });

  document.getElementById('stat-total-products').textContent = totalItems;
  document.getElementById('stat-low-stock').textContent = lowStock;

  const elInventory = document.getElementById('stat-inventory-value');
  const elRevenue = document.getElementById('stat-expected-revenue');
  if (elInventory)
    elInventory.textContent =
      totalInventoryValue.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }) + ' ج.م';
  if (elRevenue)
    elRevenue.textContent =
      totalExpectedRevenue.toLocaleString('en-US', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }) + ' ج.م';

  checkLowStockAlerts(products);

  const recentList = document.getElementById('recent-activity-list');
  recentList.innerHTML =
    recentTx
      .map(
        (tx) => `
        <div class="log-item">
            <div class="log-type">
                <i class="lucide-${tx.type === 'inbound' ? 'arrow-down-right text-success' : 'arrow-up-left text-danger'}"></i>
                <div>
                    <div style="font-weight: 600;">${tx.type === 'inbound' ? 'وارد للمخزن' : 'إذن صرف'}</div>
                    <div style="font-size: 0.75rem; color: var(--text-muted);">${new Date(tx.date).toLocaleString('ar-EG')}</div>
                </div>
            </div>
            <div class="log-details" dir="ltr" style="font-weight: 600; color: var(--text-main);">${tx.total_amount ? tx.total_amount + ' ج.م' : '-'}</div>
        </div>
    `,
      )
      .join('') ||
    '<p style="text-align: center; padding: 1rem; color: var(--text-muted);">لا توجد حركات حديثة</p>';

  renderCharts(products, transactions);

  // Render the new Expiry Radar map
  await renderExpiryRadar(products);
}

let lastNotificationTime = 0;
function checkLowStockAlerts(products) {
  const lowStockProducts = products.filter((p) => p.stock <= p.min_stock);
  if (lowStockProducts.length === 0) return;

  const now = Date.now();
  // Throttle notifications to once per hour to avoid spam
  if (now - lastNotificationTime < 3600000) return;

  if ('Notification' in window) {
    if (Notification.permission === 'granted') {
      new Notification('تنبيه نواقص المخزون', {
        body: `يوجد ${lowStockProducts.length} صنف وصلوا لحد الطلب.`,
        icon: './icons/icon-192.png',
      });
      lastNotificationTime = now;
    } else if (Notification.permission !== 'denied') {
      Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
          new Notification('تنبيه نواقص المخزون', {
            body: `يوجد ${lowStockProducts.length} صنف وصلوا لحد الطلب.`,
            icon: './icons/icon-192.png',
          });
          lastNotificationTime = now;
        }
      });
    }
  }
}

/**
 * Analytical: Expiry Radar
 */
async function renderExpiryRadar(products) {
  const radarList = document.getElementById('expiry-radar-list');
  if (!radarList) return;

  radarList.innerHTML = '';
  const now = new Date();
  const expiringItems = [];

  for (const p of products) {
    if (p.expiry_date) {
      const expDate = new Date(p.expiry_date);
      const diffTime = expDate - now;
      const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

      if (diffDays <= 90 && p.stock > 0) {
        // Only show items that are still in stock
        expiringItems.push({ ...p, daysLeft: diffDays });
      }
    }
  }

  expiringItems.sort((a, b) => a.daysLeft - b.daysLeft);

  if (expiringItems.length === 0) {
    radarList.innerHTML =
      '<div style="color: var(--text-muted); font-size: 0.85rem; text-align: center;">جميع الأصناف بصلاحيات آمنة ولا توجد بيانات مقلقة.</div>';
    if (window.lucide) window.lucide.createIcons();
    return;
  }

  expiringItems.forEach((item) => {
    let badgeClass = 'badge-danger';
    let colorStr = 'var(--text-danger)';
    let colorHex = '#ef4444'; // Red

    if (item.daysLeft > 60) {
      badgeClass = 'badge-primary';
      colorStr = '#eab308'; // Yellow
      colorHex = '#eab308';
    } else if (item.daysLeft > 30) {
      badgeClass = 'badge-warning';
      colorStr = '#f97316'; // Orange
      colorHex = '#f97316';
    }

    const div = document.createElement('div');
    div.style.padding = '0.75rem';
    div.style.background = 'rgba(255,255,255,0.02)';
    div.style.border = `1px solid ${colorHex}40`;
    div.style.borderRadius = '8px';
    div.style.display = 'flex';
    div.style.justifyContent = 'space-between';
    div.style.alignItems = 'center';

    div.innerHTML = `
            <div>
                <strong style="display:block; font-size: 0.95rem; color: var(--text-main); margin-bottom: 0.25rem;">${item.name}</strong>
                <span style="font-size: 0.75rem; color: var(--text-muted);">
                    <i data-lucide="hash" style="width: 12px; display:inline"></i> ${item.batch_number || 'N/A'} |
                    <i data-lucide="map-pin" style="width: 12px; display:inline"></i> ${item.location_code || 'غير محدد'} |
                    <i data-lucide="package" style="width: 12px; display:inline"></i> رصيد: ${item.stock}
                </span>
            </div>
            <div style="text-align: left;">
                <span class="badge" style="background: ${colorHex}20; color: ${colorHex}; border: 1px solid ${colorHex}40;">تبقي ${item.daysLeft} يوم</span>
            </div>
        `;
    radarList.appendChild(div);
  });

  if (window.lucide) window.lucide.createIcons();
}

/**
 * Chart.js Integration
 */
function renderCharts(products, transactions) {
  if (typeof Chart === 'undefined') return;
  Chart.defaults.color = '#94a3b8';
  Chart.defaults.font.family = "'Tajawal', sans-serif";

  // 1. Transaction Bar Chart (Last 7 days)
  const ctxTx = document.getElementById('txChart').getContext('2d');

  // Compute last 7 days mapping
  const last7Days = [];
  for (let i = 6; i >= 0; i--) {
    let d = new Date();
    d.setDate(d.getDate() - i);
    last7Days.push(
      d.toLocaleDateString('ar-EG', { month: 'short', day: 'numeric' }),
    );
  }
  const inboundData = new Array(7).fill(0);
  const outboundData = new Array(7).fill(0);

  const today = new Date();
  transactions.forEach((tx) => {
    const txDate = new Date(tx.date);
    const dayDiff = Math.floor((today - txDate) / (1000 * 60 * 60 * 24));
    if (dayDiff >= 0 && dayDiff < 7) {
      const index = 6 - dayDiff;
      if (tx.type === 'inbound') inboundData[index] += 1;
      else outboundData[index] += 1;
    }
  });

  if (state.charts.txChart) state.charts.txChart.destroy();
  state.charts.txChart = new Chart(ctxTx, {
    type: 'bar',
    data: {
      labels: last7Days,
      datasets: [
        {
          label: 'وارد',
          data: inboundData,
          backgroundColor: 'rgba(16, 185, 129, 0.8)',
          borderRadius: 4,
        },
        {
          label: 'منصرف',
          data: outboundData,
          backgroundColor: 'rgba(239, 68, 68, 0.8)',
          borderRadius: 4,
        },
      ],
    },
    options: {
      responsive: true,
      scales: {
        y: { beginAtZero: true, ticks: { stepSize: 1 } },
        x: { grid: { display: false } },
      },
      plugins: { legend: { position: 'top' } },
    },
  });

  // 2. Categories Doughnut Chart
  const ctxCat = document.getElementById('catChart').getContext('2d');
  const categoriesCounts = {};
  products.forEach((p) => {
    const cat = p.category || 'عام';
    categoriesCounts[cat] = (categoriesCounts[cat] || 0) + 1;
  });

  const catLabels = Object.keys(categoriesCounts);
  const catData = Object.values(categoriesCounts);

  if (state.charts.catChart) state.charts.catChart.destroy();
  state.charts.catChart = new Chart(ctxCat, {
    type: 'doughnut',
    data: {
      labels: catLabels,
      datasets: [
        {
          data: catData,
          backgroundColor: [
            '#6366f1',
            '#8b5cf6',
            '#ec4899',
            '#f43f5e',
            '#f59e0b',
            '#10b981',
            '#14b8a6',
          ],
          borderWidth: 0,
        },
      ],
    },
    options: {
      responsive: true,
      cutout: '70%',
      plugins: {
        legend: { position: 'right' },
      },
    },
  });
}

/**
 * Product Logic with Filters
 */
async function populateCategoriesFilter() {
  const products = await InventoryDB.ProductService.getAll();
  const categories = new Set(products.map((p) => p.category || 'عام'));
  const filterCat = document.getElementById('filter-category');
  // keep only first option
  filterCat.innerHTML = '<option value="">كل الفئات</option>';
  categories.forEach((cat) => {
    const opt = document.createElement('option');
    opt.value = cat;
    opt.textContent = cat;
    filterCat.appendChild(opt);
  });
}

async function renderProductsList() {
  const searchQuery = document.getElementById('product-search')?.value || '';
  const selectedCat = document.getElementById('filter-category')?.value || '';
  const selectedStock = document.getElementById('filter-stock')?.value || '';

  let products = await InventoryDB.ProductService.getAll();

  // Apply Filters
  products = products.filter((p) => {
    const matchSearch =
      p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      p.barcode.includes(searchQuery);
    const matchCat = selectedCat === '' || p.category === selectedCat;
    let matchStock = true;
    if (selectedStock === 'low') matchStock = p.stock <= p.min_stock;
    if (selectedStock === 'ok') matchStock = p.stock > p.min_stock;
    return matchSearch && matchCat && matchStock;
  });

  const currentUser = InventoryDB.AuthService.getCurrentUser();
  const canEdit = currentUser && currentUser.role !== 'worker';

  const list = document.getElementById('products-list');
  list.innerHTML =
    products
      .map(
        (p) => `
        <div class="glass list-item" style="margin-bottom: 1rem;">
            <div class="item-info">
                <h4>${p.name}</h4>
                <div style="display:flex; gap:0.5rem; flex-wrap:wrap; margin-top:0.25rem;">
                    <span style="font-family: 'Inter', sans-serif; font-size: 0.75rem;"><i data-lucide="barcode" style="width:12px;display:inline-block"></i> ${p.barcode}</span>
                    <span class="badge" style="background: rgba(255,255,255,0.1); font-size:0.65rem;">${p.category || 'عام'}</span>
                    ${p.location_code ? `<span class="badge" style="background: rgba(59, 130, 246, 0.2); color: #60a5fa; font-size:0.65rem;"><i data-lucide="map-pin" style="width:10px;display:inline-block"></i> ${p.location_code}</span>` : ''}
                    ${p.expiry_date ? `<span class="badge" style="background: rgba(239, 68, 68, 0.2); color: #f87171; font-size:0.65rem;"><i data-lucide="calendar" style="width:10px;display:inline-block"></i> ${new Date(p.expiry_date).toLocaleDateString('ar-EG')}</span>` : ''}
                </div>
            </div>
            <div class="item-actions" style="display:flex; gap:0.4rem; align-items:center; flex-wrap:wrap;">
                <div class="badge ${p.stock <= p.min_stock ? 'badge-warning' : 'badge-success'}">
                    ${p.stock} وحدة
                </div>
                ${
                  canEdit
                    ? `
                <button class="btn btn-outline" style="width:auto;padding:0.3rem 0.5rem;font-size:0.75rem;" onclick="editProduct(${p.id})">
                    <i data-lucide="pencil" style="width:14px;height:14px;"></i>
                </button>
                <button class="btn btn-outline" style="width:auto;padding:0.3rem 0.5rem;font-size:0.75rem;" onclick="printBarcode('${p.barcode}', '${p.name.replace(/'/g, "\\'")}')">
                    <i data-lucide="printer" style="width:14px;height:14px;"></i>
                </button>
                <button class="btn" style="width:auto;padding:0.3rem 0.5rem;font-size:0.75rem;background:rgba(239,68,68,0.2);color:#f87171;border:1px solid rgba(239,68,68,0.3);" onclick="deleteProduct(${p.id},'${p.name.replace(/'/g, "\\'")}')">
                    <i data-lucide="trash-2" style="width:14px;height:14px;"></i>
                </button>`
                    : ''
                }
            </div>
        </div>
    `,
      )
      .join('') ||
    '<p style="text-align: center; padding: 1rem; color: var(--text-muted);">لم يتم العثور على أصناف</p>';

  if (window.lucide) window.lucide.createIcons();
}

// Re-expose search function globally
window.renderProductsList = renderProductsList;

/**
 * Edit an existing product — loads its data into the add-product form.
 */
async function editProduct(id) {
  const product = await InventoryDB.ProductService.getAll().then((all) =>
    all.find((p) => p.id === id),
  );
  if (!product) return Swal.fire('خطأ', 'لم يتم العثور على الصنف.', 'error');

  // Populate form fields
  const f = document.getElementById('new-product-form');
  f.querySelector('[name="barcode"]').value = product.barcode || '';
  f.querySelector('[name="name"]').value = product.name || '';
  f.querySelector('[name="category"]').value = product.category || 'عام';
  f.querySelector('[name="supplier"]').value = product.supplier || 'غير محدد';
  f.querySelector('[name="location_code"]').value = product.location_code || '';
  f.querySelector('[name="price"]').value = product.price || 0;
  f.querySelector('[name="stock"]').value = product.stock || 0;
  f.querySelector('[name="min_stock"]').value = product.min_stock || 5;

  // Store the id in a hidden field
  let hiddenId = f.querySelector('#edit-product-id');
  if (!hiddenId) {
    hiddenId = document.createElement('input');
    hiddenId.type = 'hidden';
    hiddenId.id = 'edit-product-id';
    f.appendChild(hiddenId);
  }
  hiddenId.value = id;

  // Update heading & button text
  const heading = document.querySelector('#add-product h2');
  if (heading) heading.textContent = 'تعديل الصنف';
  const submitBtn = f.querySelector('button[type="submit"]');
  if (submitBtn) submitBtn.textContent = 'حفظ التعديلات';

  switchView('add-product');
}
window.editProduct = editProduct;

/**
 * Delete a product after confirmation.
 */
async function deleteProduct(id, name) {
  const result = await Swal.fire({
    title: `حذف "${name}"؟`,
    text: 'سيتم حذف الصنف نهائياً ولا يمكن التراجع.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#ef4444',
    cancelButtonColor: '#6366f1',
    confirmButtonText: 'نعم، احذفه',
    cancelButtonText: 'إلغاء',
  });
  if (!result.isConfirmed) return;

  try {
    await InventoryDB.ProductService.delete(id);
    Swal.fire('تم الحذف', `تم حذف "${name}" بنجاح.`, 'success');
    renderProductsList();
  } catch (err) {
    Swal.fire('خطأ', 'فشل حذف الصنف: ' + err.message, 'error');
  }
}
window.deleteProduct = deleteProduct;

/**
 * Excel & CSV Logic (SheetJS)
 */
async function exportToExcel() {
  try {
    const products = await InventoryDB.ProductService.getAll();
    if (products.length === 0)
      return Swal.fire('تنبيه', 'لا توجد بيانات للتصدير!', 'info');

    // Prepare data for export
    const exportData = products.map((p) => ({
      'الباركود (Barcode)': p.barcode,
      'اسم الصنف (Name)': p.name,
      'الفئة (Category)': p.category || '',
      'السعر (Price)': p.price || 0,
      'الكمية الحالية (Stock)': p.stock || 0,
      'الحد الأدنى (Min Stock)': p.min_stock || 0,
      'المورد (Supplier)': p.supplier || '',
    }));

    const worksheet = XLSX.utils.json_to_sheet(exportData);
    // Correct column widths
    worksheet['!cols'] = [
      { wch: 15 },
      { wch: 30 },
      { wch: 15 },
      { wch: 10 },
      { wch: 15 },
      { wch: 15 },
      { wch: 20 },
    ];

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'الأصناف');
    XLSX.writeFile(workbook, `Inventory_Products_${Date.now()}.xlsx`);
  } catch (err) {
    console.error(err);
    Swal.fire('خطأ', 'حدث خطأ أثناء تصدير البيانات', 'error');
  }
}
window.exportToExcel = exportToExcel;

/**
 * 🛒 Analytical: Smart Reorder Export (تصدير نواقص للطلبية)
 */
async function exportReorderListExcel() {
  try {
    const products = await InventoryDB.ProductService.getAll();
    const shortages = products.filter((p) => p.stock <= p.min_stock);

    if (shortages.length === 0) {
      return Swal.fire(
        'تنبيه',
        'لا توجد نواقص في المخزون لنظام الطلبيات حالياً.',
        'info',
      );
    }

    const data = shortages.map((p) => ({
      'الباركود (Barcode)': p.barcode,
      'اسم الصنف (Name)': p.name,
      'موقع التخزين (Bin)': p.location_code || 'غير محدد',
      'الرصيد الفعلي (Stock)': p.stock,
      'حدالطلب (Min)': p.min_stock,
      'الفئة (Category)': p.category || 'عام',
      'المورد (Supplier)': p.supplier || 'غير محدد',
      'الكمية المطلوبة (Suggested Order)': p.min_stock * 2 - p.stock, // AI Logic: Suggest ordering to cover twice the min_stock
    }));

    const ws = XLSX.utils.json_to_sheet(data);

    // Enhance formatting for neatness
    ws['!cols'] = [
      { wch: 15 },
      { wch: 30 },
      { wch: 15 },
      { wch: 15 },
      { wch: 12 },
      { wch: 15 },
      { wch: 20 },
      { wch: 25 },
    ];

    // Adding RTL support
    if (!ws['!views']) ws['!views'] = [];
    ws['!views'].push({ rightToLeft: true });

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'نواقص_للطلب_Reorder');

    const dateStr = new Date().toLocaleDateString('ar-EG').replace(/\//g, '-');
    XLSX.writeFile(wb, `طلبية_نواقص_المخزون_${dateStr}.xlsx`);

    Swal.fire(
      'تم تصدير الطلبية!',
      'تم تكوين ملف الإكسيل بنجاح وجاهز للإرسال للمورد.',
      'success',
    );
  } catch (err) {
    console.error(err);
    Swal.fire('خطأ', 'فشل تصدير القائمة', 'error');
  }
}
window.exportReorderListExcel = exportReorderListExcel;

async function handleImportCSV(e) {
  const file = e.target.files[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = async (evt) => {
    try {
      const data = evt.target.result;
      const workbook = XLSX.read(data, { type: 'binary' });
      const firstSheet = workbook.SheetNames[0];
      const rows = XLSX.utils.sheet_to_json(workbook.Sheets[firstSheet]);

      let count = 0;
      for (const row of rows) {
        // Determine keys flexibly (supports Arabic/English headers if matched)
        const barcode =
          row['الباركود (Barcode)'] || row['barcode'] || row['Barcode'];
        const name = row['اسم الصنف (Name)'] || row['name'] || row['Name'];
        if (barcode && name) {
          await InventoryDB.ProductService.add({
            barcode: String(barcode),
            name: String(name),
            category: row['الفئة (Category)'] || row['category'] || 'عام',
            price: Number(row['السعر (Price)'] || row['price'] || 0),
            stock: Number(row['الكمية الحالية (Stock)'] || row['stock'] || 0),
            min_stock: Number(
              row['الحد الأدنى (Min Stock)'] || row['min_stock'] || 5,
            ),
            supplier: row['المورد (Supplier)'] || row['supplier'] || 'غير محدد',
          });
          count++;
        }
      }
      Swal.fire({
        title: 'نجاح!',
        text: `تم استيراد ${count} صنف بنجاح.`,
        icon: 'success',
        confirmButtonText: 'موافق',
      });
      renderProductsList();
    } catch (err) {
      console.error(err);
      Swal.fire(
        'خطأ',
        'فشل قراءة الملف. تأكد من أن الملف بصيغة مدعومة.',
        'error',
      );
    }
    e.target.value = ''; // Reset input
  };
  reader.readAsBinaryString(file);
}
window.handleImportCSV = handleImportCSV;

/**
 * Backup and Restore (JSON)
 */
async function exportDatabaseBackup() {
  try {
    const data = await InventoryDB.BackupService.exportData();
    const blob = new Blob([JSON.stringify(data)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `SmartInv_Backup_${Date.now()}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  } catch (err) {
    console.error(err);
    Swal.fire('خطأ', 'فشل تصدير النسخة الاحتياطية', 'error');
  }
}
window.exportDatabaseBackup = exportDatabaseBackup;

async function importDatabaseBackup(e) {
  const file = e.target.files[0];
  if (!file) return;

  Swal.fire({
    title: 'هل أنت متأكد؟',
    text: 'استعادة النسخة الاحتياطية ستمسح كافة البيانات الحالية!',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#d33',
    cancelButtonColor: '#3085d6',
    confirmButtonText: 'نعم، استعد البيانات!',
    cancelButtonText: 'إلغاء',
  }).then((result) => {
    if (result.isConfirmed) {
      const reader = new FileReader();
      reader.onload = async (evt) => {
        try {
          const jsonData = JSON.parse(evt.target.result);
          await InventoryDB.BackupService.importData(jsonData);
          Swal.fire('نجاح', 'تم استعادة البيانات بنجاح!', 'success');
          updateDashboard();
        } catch (err) {
          console.error(err);
          Swal.fire('خطأ', 'ملف النسخة الاحتياطية غير صالح.', 'error');
        }
      };
      reader.readAsText(file);
    }
    e.target.value = ''; // Reset
  });
}
window.importDatabaseBackup = importDatabaseBackup;

/**
 * Inbound logic
 */
async function handleInbound(e) {
  e.preventDefault();
  const barcode = document.getElementById('in-barcode').value.trim();
  const qtyInput = document.getElementById('in-qty').value;
  const qty = parseInt(qtyInput, 10);
  const batchNumber = document.getElementById('in-batch').value.trim();
  const expiryDate = document.getElementById('in-expiry').value;

  const supplierSelect = document.getElementById('in-supplier');
  const supplierId = supplierSelect ? supplierSelect.value : '';
  const supplierName =
    supplierSelect && supplierSelect.selectedIndex > 0
      ? supplierSelect.options[supplierSelect.selectedIndex].text
      : '';

  const invoiceNo = document.getElementById('in-invoice')
    ? document.getElementById('in-invoice').value.trim()
    : '';

  // سعر الشراء: يستخدم المُدخَل أولاً، ثم سعر الصنف من DB كاحتياط
  const priceInput = parseFloat(
    document.getElementById('in-price')?.value || '0',
  );

  // 🛡️ التحقق الصارم من المدخلات
  if (!barcode) {
    return Swal.fire('تنبيه', 'الرجاء إدخال الباركود', 'warning');
  }
  if (isNaN(qty) || qty <= 0) {
    if (window.feedbackDevice) feedbackDevice('error');
    return Swal.fire(
      'تنبيه',
      'الرجاء إدخال كمية صحيحة (رقم موجب أكبر من الصفر)',
      'warning',
    );
  }

  const product = await InventoryDB.ProductService.getByBarcode(barcode);
  if (!product) {
    if (window.feedbackDevice) feedbackDevice('error');
    return Swal.fire(
      'تنبيه',
      'الصنف غير موجود! الرجاء إضافته أولاً من شاشة الأصناف.',
      'warning',
    );
  }

  // السعر الفعلي: المُدخَل يدوياً أولاً (أكثر دقة)، ثم سعر الصنف من DB
  const effectivePrice = priceInput > 0 ? priceInput : product.price || 0;

  try {
    // 🔒 Atomic Transaction — يضمن تحديث الكمية + تسجيل الحركة في نفس الوقت
    await InventoryDB.InventoryCoreService.executeInbound(
      product.id,
      qty,
      effectivePrice, // total_amount = effectivePrice × qty في الحركة
      batchNumber,
      expiryDate,
      supplierName,
      invoiceNo,
    );
    Swal.fire(
      'تم!',
      `✅ تمت إضافة ${qty} وحدة للمخزن بنجاح!\nالإجمالي: ${(effectivePrice * qty).toFixed(2)}`,
      'success',
    );
    e.target.reset();
  } catch (error) {
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('خطأ إداري', error.message, 'error');
  }
}

/**
 * Outbound logic
 */
async function setupOutboundView() {
  const dests = await InventoryDB.DestinationService.getAll();
  const select = document.getElementById('out-destination');
  select.innerHTML =
    `<option disabled selected value> -- اختر الجهة المستلمة -- </option>` +
    dests.map((d) => `<option value="${d.id}">${d.name}</option>`).join('');
}

async function addOutboundItemByBarcode(barcode) {
  const product = await InventoryDB.ProductService.getByBarcode(barcode);
  if (!product) {
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('تنبيه', 'الصنف غير موجود في قاعدة البيانات!', 'warning');
    return;
  }
  if (product.stock <= 0) {
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('تنبيه', 'لا توجد كمية كافية في المخزن!', 'error');
    return;
  }

  const existing = state.activeOutboundItems.find((i) => i.id === product.id);
  if (existing) {
    if (existing.qty + 1 > product.stock) {
      if (window.feedbackDevice) feedbackDevice('error');
      Swal.fire('تنبيه', 'تجاوزت الكمية المتاحة في المخزن!', 'warning');
      return;
    }
    existing.qty += 1;
  } else {
    state.activeOutboundItems.push({
      id: product.id,
      name: product.name,
      price: product.price,
      qty: 1,
      expiry_date: product.expiry_date || null,
    });

    // ⏳ نظام التنبيه FEFO إذا كان للصنف تاريخ صلاحية
    if (product.expiry_date) {
      Swal.fire({
        title: 'تنبيه (FEFO)',
        text: `هذا الصنف له تاريخ صلاحية: ${new Date(product.expiry_date).toLocaleDateString('ar-EG')}. تأكد من صرف التشغيلة الأقرب للانتهاء لتجنب الخسائر.`,
        icon: 'info',
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 5000,
      });
    }
  }
  renderOutboundList();
}

function renderOutboundList() {
  const list = document.getElementById('outbound-items-list');
  list.innerHTML = state.activeOutboundItems
    .map(
      (item, idx) => `
        <div class="glass list-item" style="margin-bottom: 0.5rem; padding: 0.75rem;">
            <div class="item-info">
                <h5 style="font-size: 0.95rem; margin-bottom: 0">${item.name}</h5>
                <div dir="ltr"><span class="financial-data">${item.price} ر.س × </span><span>${item.qty}</span></div>
                ${item.expiry_date ? `<div style="font-size:0.75rem; color:var(--text-danger);"><i data-lucide="clock" style="width:10px;display:inline-block"></i> صلاحية: ${new Date(item.expiry_date).toLocaleDateString('ar-EG')}</div>` : ''}
            </div>
            <button type="button" class="btn btn-outline" style="width: auto; padding: 0.5rem; border-color: rgba(239, 68, 68, 0.3);" onclick="removeOutboundItem(${idx})">
                <i class="lucide-trash-2 text-danger"></i>
            </button>
        </div>
    `,
    )
    .join('');
  if (window.lucide) window.lucide.createIcons();
}

window.removeOutboundItem = (index) => {
  state.activeOutboundItems.splice(index, 1);
  renderOutboundList();
};

async function finalizeOutbound() {
  if (state.activeOutboundItems.length === 0) {
    return Swal.fire('تنبيه', 'الرجاء إضافة أصناف في القائمة أولاً', 'info');
  }

  const destinationSelect = document.getElementById('out-destination');
  if (!destinationSelect.value) {
    return Swal.fire('تنبيه', 'الرجاء تحديد الجهة المستلمة', 'info');
  }

  const destinationId = destinationSelect.value;
  const destName =
    destinationSelect.options[destinationSelect.selectedIndex].text;

  try {
    // 🔒 استخدام المعاملة الكلية (Atomic Transaction) لإذن الصرف
    const tx = await InventoryDB.InventoryCoreService.executeOutbound(
      destinationId,
      destName,
      state.activeOutboundItems,
    );

    try {
      generatePDF(tx);
      Swal.fire({
        title: 'تم!',
        text: 'تم حفظ عملية الصرف وإصدار التقرير (PDF) بنجاح!',
        icon: 'success',
        confirmButtonText: 'موافق',
      });
    } catch (err) {
      console.error(err);
      Swal.fire(
        'ملاحظة',
        'تم الحفظ بنجاح، لكن حدث خطأ أثناء تكوين ملف الـ PDF.',
        'info',
      );
    }

    state.activeOutboundItems = [];
    renderOutboundList();
    destinationSelect.value = '';
  } catch (error) {
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('خطأ أثناء التنفيذ', error.message, 'error');
  }
}
window.finalizeOutbound = finalizeOutbound;

/**
 * Professional PDF Generation - jsPDF + AutoTable
 */
function generatePDF(tx) {
  const { jsPDF } = window.jspdf;
  // Set format for RTL/Arabic approximation or use standard English/Numbers if custom fonts aren't bundled.
  // To ensure PDF library doesn't crash on unrecognized utf-8, AutoTable uses built in fonts which are usually ascii/iso-8859.
  // For local solutions, this provides a professional structural layout.
  const doc = new jsPDF();

  // Header
  doc.setFillColor(30, 41, 59); // Dark blue header
  doc.rect(0, 0, 210, 40, 'F');

  doc.setTextColor(255, 255, 255);
  doc.setFont('helvetica', 'bold');
  doc.setFontSize(22);
  doc.text('SMART INVENTORY PRO', 20, 20);
  doc.setFontSize(10);
  doc.setFont('helvetica', 'normal');
  doc.text('Professional Inventory Management System', 20, 28);

  doc.setTextColor(0, 0, 0);
  doc.setFontSize(12);
  doc.setFont('helvetica', 'bold');
  doc.text('OUTBOUND ORDER / INVOICE', 20, 50);

  doc.setFontSize(10);
  doc.setFont('helvetica', 'normal');
  const orderNo = 'INV-' + Date.now().toString().slice(-6);
  doc.text(`Order Number: ${orderNo}`, 20, 60);
  doc.text(`Date: ${new Date().toLocaleString('en-US')}`, 20, 65);
  doc.text(
    `Target Destination: ${tx.destination_name} (ID: ${tx.destination_id})`,
    20,
    70,
  );

  // 🚶 WMS: Optimized Pick-Path (Sort items by Bin Location to let worker walk efficiently in straight line)
  if (tx.type === 'outbound') {
    tx.items.sort((a, b) => {
      const locA = a.location_code || 'ZZZZZ'; // items without location go to the very end
      const locB = b.location_code || 'ZZZZZ';
      return locA.localeCompare(locB);
    });
  }

  // Override the name field for offline environments without large fonts - we use the English fallback or ID
  const displayBody = tx.items.map((item, idx) => {
    const loc = item.location_code || '-';
    // jsPDF built-in fonts only support Latin; use barcode as fallback identifier
    // alongside the Arabic name so the picker can match by barcode if name renders as ?
    const nameCell = item.name || item.barcode || String(item.product_id);
    return [
      idx + 1,
      loc,
      nameCell,
      item.qty,
      (item.price || 0).toFixed(2),
      ((item.price || 0) * item.qty).toFixed(2),
    ];
  });

  doc.autoTable({
    startY: 80,
    head: [['#', 'Loc', 'Item Name', 'Qty', 'U.Price', 'Total']],
    body: displayBody,
    theme: 'striped',
    headStyles: { fillColor: [99, 102, 241], textColor: 255 }, // matches --primary color
    alternateRowStyles: { fillColor: [248, 250, 252] },
    styles: { font: 'helvetica', fontSize: 10, cellPadding: 4 },
  });

  const finalY = doc.lastAutoTable.finalY || 80;

  // Totals
  doc.setFontSize(12);
  doc.setFont('helvetica', 'bold');
  doc.text(`Total Amount: ${tx.total_amount.toFixed(2)} SR`, 140, finalY + 15);

  // Signature
  doc.setFontSize(10);
  doc.setFont('helvetica', 'normal');
  doc.text('Authorized Signature:', 20, finalY + 35);
  doc.line(20, finalY + 40, 80, finalY + 40);

  // Footer
  doc.setFontSize(8);
  doc.setTextColor(150, 150, 150);
  doc.text(
    'Generated by Smart Inventory Pro - Offline DB System',
    105,
    285,
    null,
    null,
    'center',
  );

  doc.save(`${orderNo}.pdf`);
}

/**
 * Webhook & Cloud Sync (robovai AI Agents)
 */
function saveWebhookSettings() {
  const url = document.getElementById('webhook-url').value;
  localStorage.setItem('robovai_webhook_url', url);
  Swal.fire('تم', 'تم حفظ إعدادات الـ Webhook بنجاح', 'success');
}
window.saveWebhookSettings = saveWebhookSettings;

async function triggerCloudSync() {
  const webhookUrl = localStorage.getItem('robovai_webhook_url');
  if (!webhookUrl) {
    Swal.fire('تنبيه', 'الرجاء إعداد رابط الـ Webhook أولاً', 'warning');
    return;
  }

  Swal.fire({
    title: 'جاري المزامنة...',
    text: 'يتم الآن دفع البيانات للذكاء الاصطناعي/Webhook',
    allowOutsideClick: false,
    didOpen: () => {
      Swal.showLoading();
    },
  });

  try {
    const products = await InventoryDB.ProductService.getAll();
    const transactions = await InventoryDB.TransactionService.getAll();
    const today = new Date();
    const last7DaysTx = transactions.filter((tx) => {
      const txDate = new Date(tx.date);
      const dayDiff = Math.floor((today - txDate) / (1000 * 60 * 60 * 24));
      return dayDiff >= 0 && dayDiff < 7;
    });

    const lowStockProducts = products.filter((p) => p.stock <= p.min_stock);

    const payload = {
      timestamp: new Date().toISOString(),
      metadata: {
        app_version: '2.6.0',
        device_os: navigator.platform,
      },
      inventory_summary: {
        total_products: products.length,
        low_stock_count: lowStockProducts.length,
        low_stock_details: lowStockProducts.map((p) => ({
          robovai_sync_id: p.robovai_sync_id,
          name: p.name,
          barcode: p.barcode,
          current_stock: p.stock,
          min_stock: p.min_stock,
        })),
      },
      recent_transactions: last7DaysTx.map((tx) => ({
        robovai_sync_id: tx.robovai_sync_id,
        type: tx.type,
        date: tx.date,
        total_amount: tx.total_amount,
        items_count: tx.items.length,
      })),
    };

    const response = await fetch(webhookUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });

    if (response.ok) {
      Swal.fire('نجاح', 'تمت المزامنة ودفع البيانات بنجاح', 'success');
    } else {
      Swal.fire(
        'تنبيه',
        'تم إرسال الطلب لكن السيرفر أرجع استجابة غير ناجحة',
        'warning',
      );
    }
  } catch (err) {
    console.error(err);
    Swal.fire(
      'رسالة',
      'تم محاولة الإرسال للـ Webhook (تحقق من CORS أو الاتصال بالشبكة)',
      'info',
    );
  }
}
window.triggerCloudSync = triggerCloudSync;

/**
 * Event Listeners Setup
 */
let _listenersSetUp = false;
function setupEventListeners() {
  if (_listenersSetUp) return;
  _listenersSetUp = true;

  // Bottom Navigation
  document.querySelectorAll('.nav-item').forEach((item) => {
    item.addEventListener('click', () => {
      const view = item.getAttribute('data-view');
      switchView(view);
    });
  });

  // Load existing Webhook URL
  const webhookUrlInput = document.getElementById('webhook-url');
  if (webhookUrlInput) {
    webhookUrlInput.value = localStorage.getItem('robovai_webhook_url') || '';
  }

  // New Product / Edit Product
  const productForm = document.getElementById('new-product-form');
  if (productForm) {
    productForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const formData = new FormData(e.target);
      const product = Object.fromEntries(formData.entries());
      const editId = document.getElementById('edit-product-id')?.value;

      if (editId) {
        // Edit mode
        await InventoryDB.ProductService.update(Number(editId), product);
        document.getElementById('edit-product-id').value = '';
        const heading = document.querySelector('#add-product h2');
        if (heading) heading.textContent = 'إضافة صنف جديد';
        const submitBtn = productForm.querySelector('button[type="submit"]');
        if (submitBtn) submitBtn.textContent = 'حفظ الصنف';
        Swal.fire({
          title: 'تم!',
          text: 'تم تعديل الصنف بنجاح!',
          icon: 'success',
          confirmButtonText: 'موافق',
        });
      } else {
        // Add mode
        await InventoryDB.ProductService.add(product);
        Swal.fire({
          title: 'تم!',
          text: 'تم إضافة الصنف بنجاح!',
          icon: 'success',
          confirmButtonText: 'موافق',
        });
      }

      e.target.reset();
      switchView('products');
    });
  }

  // Inbound form
  const inboundForm = document.getElementById('inbound-form');
  if (inboundForm) inboundForm.addEventListener('submit', handleInbound);

  // Global Search (with debounce)
  let searchTimeout;
  const searchInput = document.getElementById('product-search');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(renderProductsList, 300);
    });
  }

  // Add User form (admin)
  const addUserForm = document.getElementById('add-user-form');
  if (addUserForm) {
    addUserForm.addEventListener('submit', async (e) => {
      e.preventDefault();
      const username = document
        .getElementById('new-user-username')
        ?.value.trim();
      const password = document.getElementById('new-user-password')?.value;
      const role = document.getElementById('new-user-role')?.value;
      if (!username || !password || !role) return;
      try {
        await InventoryDB.AuthService.addUser(username, password, role);
        addUserForm.reset();
        await renderUsersList();
        Swal.fire({
          title: 'تم!',
          text: 'تم إضافة المستخدم بنجاح.',
          icon: 'success',
          confirmButtonText: 'موافق',
        });
      } catch (err) {
        Swal.fire('خطأ', err.message || 'فشل إضافة المستخدم.', 'error');
      }
    });
  }
}

/**
 * Scanner Control
 */
window.openScanner = (mode) => {
  state.scannerMode = mode;
  document.getElementById('scanner-modal').classList.add('active');
  ScannerManager.init('reader', (text) => processScan(text));
  ScannerManager.start();
};

window.closeScanner = () => {
  document.getElementById('scanner-modal').classList.remove('active');
  ScannerManager.stop();
};

window.handleImageScan = async (event) => {
  const file = event.target.files[0];
  if (!file) return;

  try {
    const html5QrCode = new window.Html5Qrcode('reader');
    const decodedText = await html5QrCode.scanFile(file, true);
    // Success
    processScan(decodedText);
    // Reset input
    event.target.value = '';
  } catch (err) {
    Swal.fire('خطأ', 'لم يتم العثور على باركود أو QR في هذه الصورة.', 'error');
    event.target.value = '';
  }
};

/**
 * Sensorial Feedback (Haptic & Audio)
 */
function feedbackDevice(type = 'success') {
  // Haptic Feedback
  if ('vibrate' in navigator) {
    if (type === 'success') {
      navigator.vibrate([50, 50, 50]); // Double short pulse
    } else if (type === 'error') {
      navigator.vibrate(400); // Long pulse
    }
  }

  // Audio Feedback (Beep via Web Audio API)
  try {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return;
    const ctx = new AudioContext();
    const osc = ctx.createOscillator();
    const gainNode = ctx.createGain();
    osc.connect(gainNode);
    gainNode.connect(ctx.destination);

    if (type === 'success') {
      osc.frequency.value = 1200; // High pitch successful beep
      osc.type = 'sine';
      gainNode.gain.setValueAtTime(0.1, ctx.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.1);
      osc.start(ctx.currentTime);
      osc.stop(ctx.currentTime + 0.1);
    } else if (type === 'error') {
      osc.frequency.value = 300; // Low error pitch
      osc.type = 'sawtooth';
      gainNode.gain.setValueAtTime(0.2, ctx.currentTime);
      gainNode.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.4);
      osc.start(ctx.currentTime);
      osc.stop(ctx.currentTime + 0.4);
    }
  } catch (e) {
    console.warn('Audio feedback unsupported');
  }
}
window.feedbackDevice = feedbackDevice;

function processScan(text) {
  const now = Date.now();
  // 🛡️ تطبيق حماية (Debounce) لمنع القراءات المكررة في أقل من 1.5 ثانية لنفس الباركود
  if (text === state.lastScannedCode && now - state.lastScanTime < 1500) {
    console.warn('تم تجاهل المسح السريع المتكرر للباركود: ', text);
    return;
  }

  state.lastScanTime = now;
  state.lastScannedCode = text;

  feedbackDevice('success');

  if (state.scannerMode === 'product-search') {
    document.getElementById('product-search').value = text;
    renderProductsList();
    closeScanner();
  } else if (state.scannerMode === 'inbound') {
    document.getElementById('in-barcode').value = text;
    closeScanner();
  } else if (state.scannerMode === 'outbound') {
    addOutboundItemByBarcode(text);
    // multi-scan allowed, do not close automaticlly.
  } else if (state.scannerMode === 'new-product') {
    document.getElementById('new-barcode').value = text;
    closeScanner();
  } else if (state.scannerMode === 'stocktake') {
    document.getElementById('stocktake-barcode').value = text;
    closeScanner();
    handleStocktakeManualSearch();
  }
}

/**
 * Stocktake logic (تسوية وجرد)
 */
async function initializeStocktakeView() {
  const el = document.getElementById('stocktake-barcode');
  if (el) el.value = '';
  const res = document.getElementById('stocktake-result');
  if (res) res.style.display = 'none';

  // Smart Cycle Counting Logic (AI Suggestions)
  const ccContainer = document.getElementById('cycle-counting-suggestions');
  const ccList = document.getElementById('cycle-counting-list');
  if (ccContainer && ccList) {
    const products = await InventoryDB.ProductService.getAll();
    if (products.length > 0) {
      // Logic: Pick 5 items that either haven't been counted recently or have low stock or high value.
      // We sort by last_updated (oldest first) as a simple heuristic for cycle counting.
      const suggestions = products
        .sort((a, b) => {
          const dateA = new Date(a.last_updated || 0).getTime();
          const dateB = new Date(b.last_updated || 0).getTime();
          return dateA - dateB;
        })
        .slice(0, 5);

      ccList.innerHTML = suggestions
        .map(
          (p) => `
        <div class="glass" style="display:flex; justify-content:space-between; align-items:center; padding: 0.5rem; margin-bottom: 0.5rem; cursor:pointer;" onclick="fillStocktakeBarcode('${p.barcode}')">
          <div style="flex:1;">
            <div style="font-weight:bold; font-size: 0.9rem;">${p.name}</div>
            <div style="font-size:0.75rem; color:var(--text-muted);"><i data-lucide="barcode" style="width:12px;height:12px;display:inline-block"></i> ${p.barcode}</div>
          </div>
          <div>
            <span class="badge badge-primary">الرصيد: ${p.stock}</span>
          </div>
        </div>
      `,
        )
        .join('');
      ccContainer.style.display = 'block';
      if (window.lucide) window.lucide.createIcons();
    } else {
      ccContainer.style.display = 'none';
    }
  }
}
window.initializeStocktakeView = initializeStocktakeView;

window.fillStocktakeBarcode = (barcode) => {
  const el = document.getElementById('stocktake-barcode');
  if (el) {
    el.value = barcode;
    handleStocktakeManualSearch();
  }
};

async function handleStocktakeManualSearch() {
  const barcode = document.getElementById('stocktake-barcode').value.trim();
  if (!barcode) return Swal.fire('تنبيه', 'أدخل الباركود للبحث', 'warning');

  const product = await InventoryDB.ProductService.getByBarcode(barcode);
  if (!product) {
    if (window.feedbackDevice) feedbackDevice('error');
    return Swal.fire('غير موجود', 'هذا الصنف غير مسجل بالنظام.', 'error');
  }

  const resultDiv = document.getElementById('stocktake-result');
  resultDiv.style.display = 'block';

  // Inject stocktake form
  resultDiv.innerHTML = `
        <h4 style="margin-bottom: 0.5rem; color: var(--text-main);">${product.name}</h4>
        <div style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 1rem;">
            موقع التخزين: <strong>${product.location_code || 'غير محدد'}</strong> |
            الرصيد الدفتري الحالي: <span class="badge ${product.stock <= product.min_stock ? 'badge-danger' : 'badge-primary'}">${product.stock}</span>
        </div>

        <div class="input-group">
            <label>الكمية الفعلية الموجودة على الرف</label>
            <input type="number" id="stocktake-actual-qty" placeholder="أدخل الكمية التي وجدتها..." required>
        </div>

        <div class="input-group" id="stocktake-reason-group" style="display:none;">
            <label>سبب التسوية المبدئي (إجباري عند وجود فارق)</label>
            <input type="text" id="stocktake-reason" placeholder="مثال: تالف، مفقود، خطأ في العد...">
        </div>

        <button class="btn btn-primary" onclick="submitStocktake(${product.id}, ${product.stock})">اعتماد جرد الصنف</button>
    `;

  // show reason field conditionally
  const actualQtyInput = document.getElementById('stocktake-actual-qty');
  const reasonGroupDiv = document.getElementById('stocktake-reason-group');

  actualQtyInput.addEventListener('input', (e) => {
    const val = parseInt(e.target.value, 10);
    if (!isNaN(val) && val !== product.stock) {
      reasonGroupDiv.style.display = 'block';
    } else {
      reasonGroupDiv.style.display = 'none';
    }
  });

  if (window.lucide) window.lucide.createIcons();
}
window.handleStocktakeManualSearch = handleStocktakeManualSearch;

async function submitStocktake(productId, expectedStock) {
  const actualQtyInput = document.getElementById('stocktake-actual-qty');
  const reasonInput = document.getElementById('stocktake-reason');
  const actualQty = parseInt(actualQtyInput.value, 10);

  if (isNaN(actualQty) || actualQty < 0) {
    if (window.feedbackDevice) feedbackDevice('error');
    return Swal.fire('تنبيه', 'الرجاء إدخال كمية فعلية صحيحة', 'warning');
  }

  if (actualQty === expectedStock) {
    Swal.fire(
      'ممتاز!',
      'الرصيد الفعلي يطابق الرصيد الدفتري، لا داعي للتسوية.',
      'success',
    );
    initializeStocktakeView();
    return;
  }

  const reason = reasonInput.value.trim();
  if (!reason) {
    return Swal.fire(
      'تنبيه',
      'يجب كتابة سبب التسوية لتبرير الفارق في العهدة',
      'warning',
    );
  }

  try {
    await InventoryDB.InventoryCoreService.executeAdjustment(
      productId,
      actualQty,
      reason,
    );
    Swal.fire(
      'تم التسوية',
      `تم تسجيل التسوية بنجاح (فارق ${actualQty - expectedStock}).`,
      'success',
    );
    initializeStocktakeView();
  } catch (error) {
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('خطأ في الجرد', error.message, 'error');
  }
}
window.submitStocktake = submitStocktake;

// Ensure icons refresh when data-lucide elements are added (with loop guard)
let _lucideTimer = null;
const observer = new MutationObserver((mutations) => {
  if (!window.lucide) return;
  const hasLucideNodes = mutations.some((m) =>
    Array.from(m.addedNodes).some(
      (n) =>
        n.nodeType === 1 &&
        (n.hasAttribute('data-lucide') || n.querySelector?.('[data-lucide]')),
    ),
  );
  if (hasLucideNodes) {
    clearTimeout(_lucideTimer);
    _lucideTimer = setTimeout(() => window.lucide.createIcons(), 50);
  }
});
observer.observe(document.body, { childList: true, subtree: true });

/**
 * Authentication & RBAC Logic
 */
async function handleLogin(e) {
  e.preventDefault();
  const userField = document.getElementById('login-username').value.trim();
  const passField = document.getElementById('login-password').value.trim();
  const loginBtn = document.getElementById('login-submit-btn');
  const btnText = document.getElementById('login-btn-text');
  if (loginBtn) loginBtn.disabled = true;
  if (btnText) btnText.textContent = '⏳ جاري التحقق...';

  try {
    const user = await InventoryDB.AuthService.login(userField, passField);
    document.getElementById('login-overlay').style.display = 'none';
    document.getElementById('main-app-container').style.display = 'block';
    document.getElementById('main-nav-bar').style.display = 'flex';

    setupEventListeners();
    applyPermissions(user.role);
  } catch (err) {
    if (loginBtn) loginBtn.disabled = false;
    if (btnText) btnText.textContent = 'دخول';
    if (window.feedbackDevice) feedbackDevice('error');
    Swal.fire('تسجيل الدخول مرفوض', err.message, 'error');
  }
}
window.handleLogin = handleLogin;

function handleLogout() {
  InventoryDB.AuthService.logout();
}
window.handleLogout = handleLogout;

function applyPermissions(role) {
  document.body.setAttribute('data-role', role);

  const _navDashboard = document.getElementById('nav-dashboard');
  const _navProducts = document.getElementById('nav-products');
  const _navInbound = document.getElementById('nav-inbound');
  const _navOutbound = document.getElementById('nav-outbound');
  const _navStocktake = document.getElementById('nav-stocktake');
  const _navSettings = document.getElementById('nav-settings');
  const _navTransactions = document.getElementById('nav-transactions');
  const _navKitting = document.getElementById('nav-kitting');
  const _navAudit = document.getElementById('nav-audit-logs');
  const _navDamages = document.getElementById('nav-damages');
  const _navBranches = document.getElementById('nav-branches');
  const _navSuppliers = document.getElementById('nav-suppliers');

  if (role === 'worker') {
    if (_navDashboard) _navDashboard.style.display = 'none';
    if (_navInbound) _navInbound.style.display = 'none';
    if (_navStocktake) _navStocktake.style.display = 'none';
    if (_navSettings) _navSettings.style.display = 'none';
    if (_navTransactions) _navTransactions.style.display = 'none';
    if (_navKitting) _navKitting.style.display = 'none';
    if (_navAudit) _navAudit.style.display = 'none';
    if (_navSuppliers) _navSuppliers.style.display = 'none';
    if (_navBranches) _navBranches.style.display = 'none';

    switchView('outbound');
  } else if (role === 'supervisor') {
    if (_navSettings) _navSettings.style.display = 'none';
    if (_navAudit) _navAudit.style.display = 'none';

    if (_navDashboard) _navDashboard.style.display = 'flex';
    if (_navProducts) _navProducts.style.display = 'flex';
    if (_navInbound) _navInbound.style.display = 'flex';
    if (_navOutbound) _navOutbound.style.display = 'flex';
    if (_navStocktake) _navStocktake.style.display = 'flex';
    if (_navTransactions) _navTransactions.style.display = 'flex';
    if (_navKitting) _navKitting.style.display = 'flex';
    if (_navDamages) _navDamages.style.display = 'flex';

    switchView('dashboard');
  } else {
    // admin
    if (_navDashboard) _navDashboard.style.display = 'flex';
    if (_navProducts) _navProducts.style.display = 'flex';
    if (_navInbound) _navInbound.style.display = 'flex';
    if (_navOutbound) _navOutbound.style.display = 'flex';
    if (_navStocktake) _navStocktake.style.display = 'flex';
    if (_navSettings) _navSettings.style.display = 'flex';
    if (_navTransactions) _navTransactions.style.display = 'flex';
    if (_navKitting) _navKitting.style.display = 'flex';
    if (_navAudit) _navAudit.style.display = 'flex';
    if (_navDamages) _navDamages.style.display = 'flex';
    if (_navBranches) _navBranches.style.display = 'flex';
    if (_navSuppliers) _navSuppliers.style.display = 'flex';

    const userMgmtPanel = document.getElementById('user-management-panel');
    if (userMgmtPanel) userMgmtPanel.style.display = 'block';

    switchView('dashboard');
  }
}
window.applyPermissions = applyPermissions;

/**
 * Transaction History View
 */
async function renderTransactionHistory() {
  const container = document.getElementById('transactions-list');
  if (!container) return;

  const typeFilter = document.getElementById('tx-filter-type')?.value || '';
  const dateFilter = document.getElementById('tx-filter-date')?.value || '';

  container.innerHTML =
    '<p style="text-align:center; color:var(--text-muted);">جاري تحميل الحركات...</p>';

  let transactions = await InventoryDB.TransactionService.getAll();

  // Apply filters
  if (typeFilter) {
    transactions = transactions.filter((tx) => tx.type === typeFilter);
  }
  if (dateFilter) {
    transactions = transactions.filter((tx) => {
      const txDate = new Date(tx.timestamp || tx.date || 0)
        .toISOString()
        .slice(0, 10);
      return txDate === dateFilter;
    });
  }

  // Sort newest first
  transactions.sort((a, b) => {
    const da = new Date(a.timestamp || a.date || 0).getTime();
    const db2 = new Date(b.timestamp || b.date || 0).getTime();
    return db2 - da;
  });

  if (transactions.length === 0) {
    container.innerHTML =
      '<p style="text-align:center; color:var(--text-muted); padding:2rem;">لا توجد حركات مطابقة.</p>';
    return;
  }

  const typeLabels = {
    inbound: 'وارد',
    outbound: 'صادر',
    stocktake: 'تسوية جرد',
  };
  const typeBadge = {
    inbound: 'badge-success',
    outbound: 'badge-danger',
    stocktake: 'badge-primary',
  };

  container.innerHTML = transactions
    .map((tx) => {
      const dateStr =
        tx.timestamp || tx.date
          ? new Date(tx.timestamp || tx.date).toLocaleString('ar-SA', {
              dateStyle: 'short',
              timeStyle: 'short',
            })
          : '—';
      const label = typeLabels[tx.type] || tx.type;
      const badge = typeBadge[tx.type] || 'badge-primary';
      return `
      <div class="glass log-item" style="display:flex; gap:1rem; align-items:center; padding:0.85rem 1rem; margin-bottom:0.5rem; border-radius:12px;">
        <span class="badge ${badge}" style="white-space:nowrap;">${label}</span>
        <div style="flex:1; min-width:0;">
          <div style="font-weight:600; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;">${tx.productName || tx.product_name || tx.barcode || '—'}</div>
          <div style="font-size:0.8rem; color:var(--text-muted);">${tx.barcode || ''}</div>
        </div>
        <div style="text-align:center; min-width:50px;">
          <div style="font-size:1.1rem; font-weight:700;">${tx.quantity ?? '—'}</div>
          <div style="font-size:0.75rem; color:var(--text-muted);">كمية</div>
        </div>
        <div style="text-align:start; min-width:100px;">
          <div style="font-size:0.8rem;">${dateStr}</div>
          <div style="font-size:0.75rem; color:var(--text-muted);">${tx.user || tx.username || ''}</div>
        </div>
      </div>`;
    })
    .join('');
}
window.renderTransactionHistory = renderTransactionHistory;

/**
 * User Management (Admin Only)
 */
async function renderUsersList() {
  const container = document.getElementById('users-list');
  if (!container) return;

  const currentUser = InventoryDB.AuthService.getCurrentUser();
  let users = [];
  try {
    users = await InventoryDB.AuthService.listUsers();
  } catch (err) {
    container.innerHTML =
      '<p style="color:var(--danger);">تعذّر تحميل المستخدمين.</p>';
    return;
  }

  const roleLabels = {
    super_admin: 'مدير عام',
    admin: 'مدير',
    supervisor: 'مشرف',
    worker: 'عامل',
  };
  const roleBadge = {
    super_admin: 'badge-warning',
    admin: 'badge-danger',
    supervisor: 'badge-primary',
    worker: 'badge-success',
  };

  container.innerHTML = users
    .map(
      (u) => `
    <div class="glass" style="display:flex; align-items:center; gap:1rem; padding:0.75rem 1rem; margin-bottom:0.5rem; border-radius:10px;">
      <i data-lucide="user-circle" style="width:20px;height:20px;"></i>
      <div style="flex:1; font-weight:600;">${u.username}</div>
      <span class="badge ${roleBadge[u.role] || 'badge-primary'}">${roleLabels[u.role] || u.role}</span>
      <button class="btn btn-outline" style="width:auto;padding:0.35rem 0.75rem;font-size:0.8rem;" onclick="showAuthQR('${u.username}','${u.role}')" title="توليد QR دخول">
        <i data-lucide="qr-code"></i>
      </button>
      ${
        u.username !== currentUser?.username
          ? `
        <button class="btn btn-outline" style="width:auto;padding:0.35rem 0.75rem;font-size:0.8rem;" onclick="deleteUserById(${u.id},'${u.username}')">
          <i data-lucide="trash-2"></i>
        </button>`
          : '<span style="font-size:0.75rem;color:var(--text-muted);">(أنت)</span>'
      }
    </div>`,
    )
    .join('');

  // Re-initialize lucide icons
  if (window.lucide) window.lucide.createIcons();
}

async function deleteUserById(id, username) {
  const result = await Swal.fire({
    title: `حذف المستخدم "${username}"؟`,
    text: 'لا يمكن التراجع عن هذا الإجراء.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'حذف',
    cancelButtonText: 'إلغاء',
    confirmButtonColor: '#ef4444',
  });
  if (!result.isConfirmed) return;
  try {
    await InventoryDB.AuthService.deleteUser(id);
    await renderUsersList();
    Swal.fire({
      title: 'تم!',
      text: 'تم حذف المستخدم بنجاح.',
      icon: 'success',
      confirmButtonText: 'موافق',
    });
  } catch (err) {
    Swal.fire('خطأ', err.message || 'فشل حذف المستخدم.', 'error');
  }
}
window.deleteUserById = deleteUserById;

/**
 * Enterprise WMS Features (Suppliers, Branches, Damages)
 */

// Suppliers
async function renderSuppliersList() {
  const container = document.getElementById('suppliers-list');
  const inSupplier = document.getElementById('in-supplier');
  if (!container) return;
  const suppliers = await InventoryDB.SupplierService.getAll();
  container.innerHTML =
    suppliers
      .map(
        (s) => `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05);">
      <td style="padding: 0.5rem; text-align: right;">${s.name}</td>
      <td style="padding: 0.5rem; text-align: right;" dir="ltr">${s.phone}</td>
      <td style="padding: 0.5rem; text-align: right;">
        <button class="btn btn-outline" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="deleteSupplier(${s.id})">حذف</button>
      </td>
    </tr>
  `,
      )
      .join('') ||
    '<tr><td colspan="3" style="text-align:center; padding:1rem;">لا يوجد موردين</td></tr>';

  if (inSupplier) {
    inSupplier.innerHTML =
      '<option value="">-- اختر المورد --</option>' +
      suppliers
        .map((s) => `<option value="${s.id}">${s.name}</option>`)
        .join('');
  }
}
window.renderSuppliersList = renderSuppliersList;

window.deleteSupplier = async (id) => {
  if (confirm('هل أنت متأكد من الحذف؟')) {
    await InventoryDB.SupplierService.delete(id);
    renderSuppliersList();
  }
};

// Branches
async function renderBranchesList() {
  const container = document.getElementById('branches-list');
  const outSelect = document.getElementById('out-destination');
  if (!container) return;
  const branches = await InventoryDB.BranchService.getAll();

  container.innerHTML =
    branches
      .map(
        (b) => `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05);">
      <td style="padding: 0.5rem; text-align: right;">${b.name}</td>
      <td style="padding: 0.5rem; text-align: right;">
        <button class="btn btn-outline" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="deleteBranch(${b.id})">حذف</button>
      </td>
    </tr>
  `,
      )
      .join('') ||
    '<tr><td colspan="2" style="text-align:center; padding:1rem;">لا يوجد فروع مسجلة</td></tr>';

  if (outSelect) {
    outSelect.innerHTML =
      '<option value="">-- اختر الفرع المستلم --</option>' +
      branches
        .map((b) => `<option value="${b.name}">${b.name}</option>`)
        .join('');
  }

  // Also render transfers when rendering branches
  renderTransfersList();
}
window.renderBranchesList = renderBranchesList;

async function renderTransfersList() {
  const container = document.getElementById('transfers-list');
  if (!container) return;
  const transfers = await InventoryDB.TransferService.getAll();

  container.innerHTML =
    transfers
      .map(
        (t) => `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05); font-size: 0.85rem;">
      <td style="padding: 0.5rem; text-align: right;">${new Date(t.date).toLocaleString('ar-EG')}</td>
      <td style="padding: 0.5rem; text-align: right;">#${t.transaction_id}</td>
      <td style="padding: 0.5rem; text-align: right;">${t.destination_name}</td>
      <td style="padding: 0.5rem; text-align: right;">
        <span class="badge ${t.status === 'in-transit' ? 'badge-warning' : 'badge-success'}">
          ${t.status === 'in-transit' ? 'قيد النقل' : 'مستلم'}
        </span>
      </td>
      <td style="padding: 0.5rem; text-align: right;">
        ${
          t.status === 'in-transit'
            ? `
        <button class="btn btn-outline" style="padding: 0.25rem 0.5rem; font-size: 0.75rem;" onclick="markTransferReceived(${t.id})">
          <i data-lucide="check-circle" style="width:14px;height:14px;display:inline-block"></i> تأكيد الاستلام
        </button>
        `
            : '-'
        }
      </td>
    </tr>
  `,
      )
      .join('') ||
    '<tr><td colspan="5" style="text-align:center; padding:1rem;">لا توجد بضائع قيد النقل</td></tr>';

  if (window.lucide) window.lucide.createIcons();
}
window.renderTransfersList = renderTransfersList;

window.markTransferReceived = async (id) => {
  if (confirm('تأكيد استلام البضاعة في الفرع؟')) {
    await InventoryDB.TransferService.markReceived(id);
    renderTransfersList();
    Swal.fire('تم', 'تم تحديث حالة البضاعة إلى مستلمة', 'success');
  }
};

window.deleteBranch = async (id) => {
  if (confirm('هل أنت متأكد من الحذف؟')) {
    await InventoryDB.BranchService.delete(id);
    renderBranchesList();
  }
};

window.printBarcode = (barcode, name) => {
  const printWindow = window.open('', '_blank', 'width=600,height=400');
  printWindow.document.write(`
    <html>
      <head>
        <title>طباعة باركود</title>
        <style>
          body { font-family: sans-serif; text-align: center; padding: 20px; }
          .label { border: 1px solid #ccc; padding: 10px; display: inline-block; border-radius: 8px; }
          .name { font-weight: bold; margin-bottom: 5px; font-size: 14px; }
          @media print {
            body { padding: 0; }
            .label { border: none; }
          }
        </style>
      </head>
      <body>
        <div class="label">
          <div class="name">${name}</div>
          <svg id="barcode"></svg>
        </div>
        <script src="https://cdn.jsdelivr.net/npm/jsbarcode@3.11.5/dist/JsBarcode.all.min.js"></script>
        <script>
          window.onload = function() {
            JsBarcode("#barcode", "${barcode}", {
              format: "CODE128",
              width: 2,
              height: 50,
              displayValue: true
            });
            setTimeout(() => {
              window.print();
              window.close();
            }, 500);
          }
        </script>
      </body>
    </html>
  `);
  printWindow.document.close();
};

// Damages
async function renderDamagesList() {
  const container = document.getElementById('damages-list');
  if (!container) return;
  const damages = await InventoryDB.DamageService.getAll();

  container.innerHTML =
    damages
      .map(
        (d) => `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05);">
      <td style="padding: 0.5rem; text-align: right;">${new Date(d.date).toLocaleDateString('ar-EG')}</td>
      <td style="padding: 0.5rem; text-align: right;">${d.barcode}</td>
      <td style="padding: 0.5rem; text-align: right;">${d.quantity}</td>
      <td style="padding: 0.5rem; text-align: right;"><span class="badge badge-danger">${d.reason}</span></td>
    </tr>
  `,
      )
      .join('') ||
    '<tr><td colspan="4" style="text-align:center; padding:1rem;">لا توجد سجلات توالف</td></tr>';
}
window.renderDamagesList = renderDamagesList;

// Audit Logs
async function renderAuditLogs() {
  const container = document.getElementById('audit-logs-list');
  if (!container) return;
  const logs = await InventoryDB.AuditService.getAll();

  container.innerHTML =
    logs
      .map((log) => {
        let badgeClass = 'badge-primary';
        if (log.action === 'CREATE') badgeClass = 'badge-success';
        if (log.action === 'DELETE') badgeClass = 'badge-danger';

        return `
    <tr style="border-bottom: 1px solid rgba(255,255,255,0.05); font-size: 0.8rem;">
      <td style="padding: 0.5rem; text-align: right;">${new Date(log.date).toLocaleString('ar-EG')}</td>
      <td style="padding: 0.5rem; text-align: right;">${log.user}</td>
      <td style="padding: 0.5rem; text-align: right;"><span class="badge ${badgeClass}">${log.action}</span></td>
      <td style="padding: 0.5rem; text-align: right;">${log.entity} (#${log.entity_id})</td>
      <td style="padding: 0.5rem; text-align: right; max-width: 200px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title='${log.new_data || log.old_data}'>
        ${log.new_data || log.old_data || 'N/A'}
      </td>
    </tr>
  `;
      })
      .join('') ||
    '<tr><td colspan="5" style="text-align:center; padding:1rem;">لا توجد سجلات بعد</td></tr>';
}
window.renderAuditLogs = renderAuditLogs;

// Kitting
async function renderKittingView() {
  const select = document.getElementById('kit-component-select');
  if (!select) return;
  const products = await InventoryDB.ProductService.getAll();
  select.innerHTML =
    '<option value="">-- اختر صنف --</option>' +
    products
      .map(
        (p) =>
          `<option value="${p.id}" data-name="${p.name}" data-price="${p.price}">${p.name} (${p.stock} متاح)</option>`,
      )
      .join('');
}
window.renderKittingView = renderKittingView;

let kitComponents = [];
window.addKitComponent = () => {
  const select = document.getElementById('kit-component-select');
  const qtyInput = document.getElementById('kit-component-qty');
  const id = select.value;
  const qty = parseInt(qtyInput.value);
  if (!id || !qty || qty <= 0)
    return Swal.fire('خطأ', 'الرجاء اختيار صنف وتحديد كمية صحيحة', 'error');

  const option = select.options[select.selectedIndex];
  const name = option.dataset.name;

  kitComponents.push({ id: parseInt(id), name, qty });
  renderKitComponentsList();
};

function renderKitComponentsList() {
  const list = document.getElementById('kit-components-list');
  if (kitComponents.length === 0) {
    list.innerHTML =
      '<li style="color: var(--text-muted); font-size: 0.85rem; text-align: center; list-style: none;">لا يوجد مكونات مضافة بعد</li>';
    return;
  }

  list.innerHTML = kitComponents
    .map(
      (c, index) => `
    <li style="display: flex; justify-content: space-between; margin-bottom: 0.5rem; padding-bottom: 0.5rem; border-bottom: 1px solid rgba(255,255,255,0.05);">
      <span>${c.name}</span>
      <span>${c.qty} <button type="button" class="btn btn-outline" style="padding: 0 0.5rem; color: #ef4444;" onclick="removeKitComponent(${index})">X</button></span>
    </li>
  `,
    )
    .join('');
}
window.removeKitComponent = (index) => {
  kitComponents.splice(index, 1);
  renderKitComponentsList();
};

/**
 * Cloud Account Registration Handler
 */
window.registerCloudAccount = async function (e) {
  e.preventDefault();
  const storeName = document.getElementById('cloud-store-name').value.trim();
  const email = document.getElementById('cloud-email').value.trim();
  const password = document.getElementById('cloud-password').value;

  const btn = document.getElementById('cloud-setup-submit-btn');
  if (btn) {
    btn.disabled = true;
    btn.textContent = 'جاري التفعيل...';
  }

  try {
    await InventoryDB.FirebaseService.registerAccount(
      storeName,
      email,
      password,
    );
    document.getElementById('cloud-setup-overlay').style.display = 'none';
    updateCloudStatusBadge();
    Swal.fire({
      title: '✅ تم!',
      text: 'تم تفعيل المزامنة السحابية بنجاح! بياناتك ستُزامَن تلقائياً عبر جميع أجهزتك.',
      icon: 'success',
      confirmButtonText: 'رائع!',
    });
  } catch (err) {
    Swal.fire('خطأ في التسجيل', err.message, 'error');
  } finally {
    if (btn) {
      btn.disabled = false;
      btn.textContent = 'تفعيل المزامنة السحابية';
    }
  }
};

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}

// ---------------------------------------------------------------------------
// Factory Reset
// ---------------------------------------------------------------------------
async function confirmFactoryReset() {
  const result = await Swal.fire({
    title: 'إعادة ضبط المصنع',
    html: '<p>هل أنت متأكد؟ سيتم حذف <strong>جميع البيانات</strong> بشكل نهائي.</p><p style="color:var(--danger);font-weight:600;">لا يمكن التراجع عن هذا الإجراء.</p>',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'نعم، أعد الضبط',
    cancelButtonText: 'إلغاء',
    confirmButtonColor: '#ef4444',
  });
  if (!result.isConfirmed) return;

  const confirm2 = await Swal.fire({
    title: 'تأكيد أخير',
    text: 'اكتب "مسح" للتأكيد',
    input: 'text',
    inputPlaceholder: 'مسح',
    showCancelButton: true,
    confirmButtonText: 'تأكيد',
    cancelButtonText: 'إلغاء',
    confirmButtonColor: '#ef4444',
    preConfirm: (val) => {
      if (val !== 'مسح') Swal.showValidationMessage('اكتب كلمة "مسح" للتأكيد');
    },
  });
  if (!confirm2.isConfirmed) return;

  try {
    const d = window.InventoryDB?.db;
    if (d) {
      await Promise.all(
        [
          d.products?.clear(),
          d.transactions?.clear(),
          d.destinations?.clear(),
          d.users?.clear(),
          d.suppliers?.clear(),
          d.branches?.clear(),
          d.damages?.clear(),
          d.audit_logs?.clear(),
          d.kits?.clear(),
          d.transfers?.clear(),
        ].filter(Boolean),
      );
    }

    [
      'robovai_user',
      'robovai_account_id',
      'robovai_qr_secret',
      'robovai_paired_pos',
      'robovai_webhook_url',
    ].forEach((k) => localStorage.removeItem(k));

    if (window.InventoryDB?.FirebaseService?.signOut) {
      try {
        await window.InventoryDB.FirebaseService.signOut();
      } catch (_) {
        /* ignore */
      }
    }

    await Swal.fire('تم', 'تم إعادة ضبط المصنع بنجاح.', 'success');
    window.location.reload();
  } catch (err) {
    Swal.fire('خطأ', 'حدث خطأ أثناء إعادة الضبط: ' + err.message, 'error');
  }
}
window.confirmFactoryReset = confirmFactoryReset;
