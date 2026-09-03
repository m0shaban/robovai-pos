/**
 * RobovAI Executive Owner Dashboard JS
 * متابعة المبيعات والتقارير المالية المباشرة لحظة بلحظة مع التنبيهات الصوتية
 */

let _ownerState = {
  cloudServerUrl: 'http://localhost:7895',
  wsUrl: 'ws://localhost:7895',
  wsClient: null,
  autoRefreshTimer: null,
  soundEnabled: true,
  liveSalesFeed: [],
};

// ─── WebSocket Live Listener ──────────────────────────────────────────────────

function initOwnerWebSocket() {
  if (_ownerState.wsClient) {
    try { _ownerState.wsClient.close(); } catch {}
  }

  try {
    const ws = new WebSocket(_ownerState.wsUrl);
    _ownerState.wsClient = ws;

    updateWsStatusUI('connecting');

    ws.onopen = () => {
      console.log('[OwnerWS] Connected to Cloud Server');
      updateWsStatusUI('online');
    };

    ws.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        handleOwnerWsEvent(data);
      } catch (err) {
        console.error('[OwnerWS] Error parsing message:', err);
      }
    };

    ws.onclose = () => {
      updateWsStatusUI('offline');
      // Reconnect after 5 seconds
      setTimeout(initOwnerWebSocket, 5000);
    };

    ws.onerror = () => {
      updateWsStatusUI('offline');
    };
  } catch (err) {
    updateWsStatusUI('offline');
  }
}

function handleOwnerWsEvent(data) {
  const { event, payload } = data;

  if (event === 'NEW_SALE') {
    // Play subtle audio alert if enabled
    if (_ownerState.soundEnabled) {
      playSaleChime();
    }

    // Add to live sales feed
    _ownerState.liveSalesFeed.unshift(payload);
    if (_ownerState.liveSalesFeed.length > 20) _ownerState.liveSalesFeed.pop();

    // Render toast & refresh dashboard stats
    showSaleToast(payload);
    refreshOwnerDashboard();
  }
}

function updateWsStatusUI(status) {
  const badge = document.getElementById('owner-ws-badge');
  if (!badge) return;

  if (status === 'online') {
    badge.style.background = 'rgba(16,185,129,0.15)';
    badge.style.color = '#10b981';
    badge.innerHTML = '● بث مباشر أونلاين';
  } else if (status === 'connecting') {
    badge.style.background = 'rgba(245,158,11,0.15)';
    badge.style.color = '#f59e0b';
    badge.innerHTML = '⏳ جاري الاتصال...';
  } else {
    badge.style.background = 'rgba(239,68,68,0.15)';
    badge.style.color = '#ef4444';
    badge.innerHTML = '○ أوفلاين (إعادة محاولة)';
  }
}

function playSaleChime() {
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = 'sine';
    osc.frequency.setValueAtTime(587.33, ctx.currentTime); // D5 note
    osc.frequency.exponentialRampToValueAtTime(880, ctx.currentTime + 0.15); // A5 note
    gain.gain.setValueAtTime(0.15, ctx.currentTime);
    gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.3);
    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start();
    osc.stop(ctx.currentTime + 0.3);
  } catch {}
}

function showSaleToast(sale) {
  if (typeof Swal !== 'undefined') {
    const Toast = Swal.mixin({
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: 3500,
      timerProgressBar: true,
      background: '#1e293b',
      color: '#f8fafc',
    });
    Toast.fire({
      icon: 'success',
      title: `🛍️ عملية بيع جديدة!`,
      html: `<b>${Number(sale.totalAmount).toLocaleString()} ج.م</b> · ${sale.branchName || 'الكاشير'}<br><small style="color:#94a3b8;">فاتورة: ${sale.invoiceNumber}</small>`,
    });
  }
}

// ─── Dashboard REST Fetch ─────────────────────────────────────────────────────

async function refreshOwnerDashboard() {
  try {
    const res = await fetch(`${_ownerState.cloudServerUrl}/api/cloud/dashboard`);
    if (!res.ok) throw new Error('فشل الاتصال بالسيرفر السحابي');
    const data = await res.json();

    renderOwnerKPIs(data);
    renderOwnerPaymentBreakdown(data.paymentMethods || {});
    renderOwnerBranches(data.branches || []);
    renderOwnerRecentSales(data.recentSales || []);

  } catch (err) {
    console.warn('[OwnerDashboard] Offline or server unreachable:', err.message);
  }
}

function renderOwnerKPIs(data) {
  const revEl = document.getElementById('owner-total-revenue');
  const txEl = document.getElementById('owner-total-tx');
  const avgEl = document.getElementById('owner-avg-ticket');

  if (revEl) revEl.textContent = `${Number(data.totalRevenue || 0).toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م`;
  if (txEl) txEl.textContent = (data.totalTransactions || 0).toLocaleString('ar-EG');
  if (avgEl) avgEl.textContent = `${Number(data.avgTicket || 0).toLocaleString('ar-EG', { minimumFractionDigits: 2 })} ج.م`;
}

function renderOwnerPaymentBreakdown(payments) {
  const cashEl = document.getElementById('owner-pay-cash');
  const cardEl = document.getElementById('owner-pay-card');
  const vodaEl = document.getElementById('owner-pay-voda');
  const instaEl = document.getElementById('owner-pay-insta');

  if (cashEl) cashEl.textContent = `${Number(payments.cash || 0).toLocaleString()} ج.م`;
  if (cardEl) cardEl.textContent = `${Number(payments.card || 0).toLocaleString()} ج.م`;
  if (vodaEl) vodaEl.textContent = `${Number(payments.vodafone || 0).toLocaleString()} ج.م`;
  if (instaEl) instaEl.textContent = `${Number(payments.instapay || 0).toLocaleString()} ج.م`;
}

function renderOwnerBranches(branches) {
  const container = document.getElementById('owner-branch-list');
  if (!container) return;

  if (branches.length === 0) {
    container.innerHTML = '<div style="text-align:center;padding:1rem;color:var(--text-muted);">لا توجد فروع متصلة حالياً</div>';
    return;
  }

  container.innerHTML = branches.map(b => `
    <div style="display:flex;justify-content:space-between;align-items:center;padding:0.75rem 1rem;background:rgba(255,255,255,0.03);border-radius:0.75rem;margin-bottom:0.5rem;">
      <div>
        <div style="font-weight:600;font-size:0.9rem;">${escapeHtmlOwner(b.name)}</div>
        <div style="font-size:0.78rem;color:var(--text-muted);">${b.count} معاملة بيع اليوم</div>
      </div>
      <div style="font-size:1rem;font-weight:700;color:var(--success);">
        ${Number(b.total).toLocaleString()} ج.م
      </div>
    </div>
  `).join('');
}

function renderOwnerRecentSales(sales) {
  const container = document.getElementById('owner-recent-feed');
  if (!container) return;

  if (sales.length === 0) {
    container.innerHTML = '<div style="text-align:center;padding:1.5rem;color:var(--text-muted);">لا توجد عمليات بيع اليوم حتى الآن</div>';
    return;
  }

  container.innerHTML = sales.map(s => `
    <div class="log-item" style="padding:0.75rem 0;border-bottom:1px solid var(--border);">
      <div class="log-type">
        <span style="display:inline-block;padding:0.25rem 0.5rem;border-radius:6px;background:rgba(99,102,241,0.15);color:#818cf8;font-size:0.75rem;font-weight:600;">
          ${escapeHtmlOwner(s.branchName || 'الكاشير')}
        </span>
        <div>
          <div style="font-weight:600;">${escapeHtmlOwner(s.invoiceNumber)}</div>
          <div style="font-size:0.75rem;color:var(--text-muted);">${s.cashierName || 'كاشير'} · ${s.paymentMethod || 'نقداً'}</div>
        </div>
      </div>
      <div style="text-align:left;">
        <div style="font-weight:700;color:var(--success);">${Number(s.totalAmount).toLocaleString()} ج.م</div>
        <div style="font-size:0.72rem;color:var(--text-muted);">${formatOwnerTime(s.saleDate || s.syncedAt)}</div>
      </div>
    </div>
  `).join('');
}

function escapeHtmlOwner(str) {
  if (!str) return '';
  return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function formatOwnerTime(isoString) {
  if (!isoString) return '';
  try {
    const d = new Date(isoString);
    return d.toLocaleTimeString('ar-EG', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  } catch {
    return isoString;
  }
}

// ─── Initialize ───────────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
  initOwnerWebSocket();
  refreshOwnerDashboard();

  // Auto-refresh REST every 15 seconds
  _ownerState.autoRefreshTimer = setInterval(refreshOwnerDashboard, 15000);
});
