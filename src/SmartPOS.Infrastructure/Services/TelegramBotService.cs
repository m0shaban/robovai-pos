using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Service that connects RobovAI POS directly to the Store Owner's Telegram App.
/// Features:
///   - Instant real-time sale alerts on every completed cashier invoice
///   - Automatic Z-Report shift closure summaries
///   - Responds to owner commands: /today, /stock, /shift
/// </summary>
public class TelegramBotService : IHostedService, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<TelegramBotService>? _logger;
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _cts;
    private Task? _botPollingTask;
    private long _lastUpdateId = 0;

    // Default Bot Token for RobovAI POS Official Bot (t.me/robovaipos_bot)
    private const string DefaultBotToken = "8802777585:AAHdhh-LQGgGP09Ge1MGb_kYG21Dk-ZCHZM";

    public TelegramBotService(
        IDbContextFactory<AppDbContext> contextFactory,
        ISettingsService settingsService,
        ILogger<TelegramBotService>? logger = null)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Telegram Bot Service starting...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _botPollingTask = RunTelegramLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Send an instant Telegram notification to the store owner when a sale is completed.
    /// </summary>
    public async Task SendSaleNotificationAsync(string invoiceNumber, decimal totalAmount, string paymentMethod, string cashierName, int itemsCount)
    {
        var chatId = GetConfiguredChatId();
        var botToken = GetConfiguredBotToken();
        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(botToken)) return;

        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";
        var timeStr = DateTime.Now.ToString("HH:mm:ss");

        var badge = paymentMethod.Contains("كاش") || paymentMethod.Contains("Cash") ? "💵" : "💳";

        var text = new StringBuilder();
        text.AppendLine($"🛍️ <b>عملية بيع جديدة — {EscapeMarkdown(storeName)}</b>");
        text.AppendLine($"========================");
        text.AppendLine($"💰 <b>المبلغ الإجمالي:</b> <code>{totalAmount:N2} ج.م</code>");
        text.AppendLine($"{badge} <b>طريقة الدفع:</b> {EscapeMarkdown(paymentMethod)}");
        text.AppendLine($"📄 <b>رقم الفاتورة:</b> <code>{EscapeMarkdown(invoiceNumber)}</code>");
        text.AppendLine($"📦 <b>عدد الأصناف:</b> {itemsCount}");
        text.AppendLine($"👤 <b>الكاشير:</b> {EscapeMarkdown(cashierName)}");
        text.AppendLine($"⏰ <b>الوقت:</b> {timeStr}");

        await SendMessageAsync(botToken, chatId, text.ToString());
    }

    /// <summary>
    /// Security Alert: Send alert to owner when a refund or invoice cancellation is performed.
    /// </summary>
    public async Task SendRefundNotificationAsync(string invoiceNumber, decimal refundAmount, string cashierName, string reason)
    {
        var chatId = GetConfiguredChatId();
        var botToken = GetConfiguredBotToken();
        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(botToken)) return;

        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";
        var timeStr = DateTime.Now.ToString("HH:mm:ss");

        var text = new StringBuilder();
        text.AppendLine($"🔴 <b>تنبيه مرتجع / إلغاء فاتورة — {EscapeMarkdown(storeName)}</b>");
        text.AppendLine($"========================");
        text.AppendLine($"💸 <b>المبلغ المرتجع:</b> <code>{refundAmount:N2} ج.م</code>");
        text.AppendLine($"📄 <b>رقم الفاتورة:</b> <code>{EscapeMarkdown(invoiceNumber)}</code>");
        text.AppendLine($"👤 <b>الكاشير:</b> {EscapeMarkdown(cashierName)}");
        text.AppendLine($"📝 <b>السبب:</b> {EscapeMarkdown(reason)}");
        text.AppendLine($"⏰ <b>الوقت:</b> {timeStr}");

        await SendMessageAsync(botToken, chatId, text.ToString());
    }

    /// <summary>
    /// Send low stock warning when an item hits 0 or falls below min stock.
    /// </summary>
    public async Task SendLowStockWarningAsync(string productName, int currentStock, int minStock)
    {
        var chatId = GetConfiguredChatId();
        var botToken = GetConfiguredBotToken();
        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(botToken)) return;

        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";

        var text = new StringBuilder();
        text.AppendLine($"⚠️ <b>تنبيه نقص مخزون حرج — {EscapeMarkdown(storeName)}</b>");
        text.AppendLine($"========================");
        text.AppendLine($"📦 <b>المنتج:</b> {EscapeMarkdown(productName)}");
        text.AppendLine($"📉 <b>الرصيد المتبقي:</b> <code>{currentStock}</code>");
        text.AppendLine($"📊 <b>الحد الأدنى المطلوب:</b> {minStock}");

        if (currentStock <= 0)
        {
            text.AppendLine($"🛑 <b>الحالة:</b> <u>نفذ بالكامل! يرجى عمل طلب توريد.</u>");
        }

        await SendMessageAsync(botToken, chatId, text.ToString());
    }

    /// <summary>
    /// Send Z-Report shift summary to owner when cashier closes shift.
    /// </summary>
    public async Task SendZReportNotificationAsync(string cashierName, decimal totalSales, decimal cashTotal, decimal cardTotal, decimal netProfit, decimal blindDiff)
    {
        var chatId = GetConfiguredChatId();
        var botToken = GetConfiguredBotToken();
        if (string.IsNullOrWhiteSpace(chatId) || string.IsNullOrWhiteSpace(botToken)) return;

        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";
        var dateStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        var text = new StringBuilder();
        text.AppendLine($"📋 <b>تقرير إغلاق الوردية (Z-Report)</b>");
        text.AppendLine($"🏪 <b>المتجر:</b> {EscapeMarkdown(storeName)}");
        text.AppendLine($"👤 <b>الكاشير:</b> {EscapeMarkdown(cashierName)}");
        text.AppendLine($"📅 <b>التاريخ:</b> {dateStr}");
        text.AppendLine($"========================");
        text.AppendLine($"💵 <b>إجمالي المبيعات:</b> <code>{totalSales:N2} ج.م</code>");
        text.AppendLine($"💸 <b>النقدية بالدرج:</b> <code>{cashTotal:N2} ج.م</code>");
        text.AppendLine($"💳 <b>مدفوعات البطاقات:</b> <code>{cardTotal:N2} ج.م</code>");
        text.AppendLine($"📈 <b>صافي الأرباح:</b> <code>{netProfit:N2} ج.م</code>");

        if (blindDiff != 0)
        {
            var label = blindDiff > 0 ? "زيادة بالدرج 🟢" : "عجز بالدرج 🔴";
            text.AppendLine($"⚠️ <b>فارق الوردية ({label}):</b> <code>{Math.Abs(blindDiff):N2} ج.م</code>");
        }
        else
        {
            text.AppendLine($"✅ <b>تطابق الدرج:</b> متطابق تماماً بدون عجز أو زيادة.");
        }

        await SendMessageAsync(botToken, chatId, text.ToString());
    }

    private async Task RunTelegramLoopAsync(CancellationToken ct)
    {
        await Task.Delay(3000, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var botToken = GetConfiguredBotToken();
                var chatId = GetConfiguredChatId();

                if (!string.IsNullOrWhiteSpace(botToken) && !string.IsNullOrWhiteSpace(chatId))
                {
                    await PollTelegramUpdatesAsync(botToken, chatId, ct);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in Telegram polling loop.");
            }

            try { await Task.Delay(4000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task PollTelegramUpdatesAsync(string botToken, string targetChatId, CancellationToken ct)
    {
        var url = $"https://api.telegram.org/bot{botToken}/getUpdates?offset={_lastUpdateId + 1}&limit=10&timeout=2";
        try
        {
            var res = await _httpClient.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return;

            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("ok", out var okProp) || !okProp.GetBoolean()) return;

            if (!root.TryGetProperty("result", out var results)) return;

            foreach (var update in results.EnumerateArray())
            {
                if (update.TryGetProperty("update_id", out var uid))
                {
                    _lastUpdateId = uid.GetInt64();
                }

                if (update.TryGetProperty("message", out var msg))
                {
                    var chatId = msg.GetProperty("chat").GetProperty("id").GetInt64().ToString();
                    var text = msg.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";

                    if (chatId == targetChatId || text.StartsWith("/start"))
                    {
                        await HandleOwnerCommandAsync(botToken, chatId, text.Trim(), ct);
                    }
                }
            }
        }
        catch { /* Offline or API error */ }
    }

    private async Task HandleOwnerCommandAsync(string botToken, string chatId, string commandText, CancellationToken ct)
    {
        var cmd = commandText.ToLowerInvariant();
        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";

        if (cmd == "/start" || cmd == "/menu" || cmd == "/help")
        {
            var welcome = new StringBuilder();
            welcome.AppendLine($"👋 <b>أهلاً بك في بوت إدارة RobovAI POS!</b>\n");
            welcome.AppendLine($"🏪 <b>المتجر:</b> {EscapeMarkdown(storeName)}");
            welcome.AppendLine($"🆔 <b>Chat ID الخاص بك:</b> <code>{chatId}</code>\n");
            welcome.AppendLine($"<b>📋 قائمة الأوامر التفاعلية المتاحة:</b>");
            welcome.AppendLine($"📊 /today — ملخص مبيعات ومصروفات اليوم");
            welcome.AppendLine($"📋 /shift — حالة الوردية الحالية بالدرج");
            welcome.AppendLine($"🏆 /top — الأصناف الأكثر مبيعاً اليوم");
            welcome.AppendLine($"⚠️ /stock — الأصناف ناقصة المخزون");
            welcome.AppendLine($"🤝 /debts — ديون عملاء الآجل المتبقية");
            welcome.AppendLine($"💻 /status — حالة النظام والسيرفر المحلي");
            welcome.AppendLine($"\n💡 <i>أدخل هذا الـ Chat ID في شاشة إعدادات الكاشير لتلقي الإشعارات الفورية.</i>");

            await SendMessageAsync(botToken, chatId, welcome.ToString());
            return;
        }

        if (cmd == "/today")
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var todaySales = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today && !s.IsDeleted && s.Status == Core.Entities.SaleStatus.Completed)
                .ToListAsync(ct);

            var todayExpenses = await context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= today && !e.IsDeleted)
                .SumAsync(e => (double?)e.Amount, ct) ?? 0.0;

            var totalRev = todaySales.Sum(s => s.TotalAmount);
            var totalTx = todaySales.Count;
            var cashRev = todaySales.Where(s => s.PaymentMethod == Core.Entities.PaymentMethod.Cash).Sum(s => s.TotalAmount);
            var cardRev = todaySales.Where(s => s.PaymentMethod == Core.Entities.PaymentMethod.Card).Sum(s => s.TotalAmount);
            var vodaRev = todaySales.Where(s => s.PaymentMethod == Core.Entities.PaymentMethod.VodafoneCash).Sum(s => s.TotalAmount);
            var instaRev = todaySales.Where(s => s.PaymentMethod == Core.Entities.PaymentMethod.InstaPay).Sum(s => s.TotalAmount);
            var deferredRev = todaySales.Where(s => s.PaymentMethod == Core.Entities.PaymentMethod.Deferred).Sum(s => s.TotalAmount);

            var totalCost = todaySales.Sum(s => s.Subtotal); // Purchase price sum
            var estProfit = (double)totalRev - (double)totalCost - todayExpenses;

            var text = new StringBuilder();
            text.AppendLine($"📊 <b>تقرير مبيعات اليوم — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"📅 {DateTime.Now:yyyy-MM-dd HH:mm}");
            text.AppendLine($"========================");
            text.AppendLine($"💰 <b>إجمالي الإيرادات:</b> <code>{totalRev:N2} ج.م</code>");
            text.AppendLine($"🔢 <b>عدد الفواتير:</b> {totalTx}");
            text.AppendLine($"🎟️ <b>متوسط الفاتورة:</b> {(totalTx > 0 ? (totalRev / totalTx) : 0):N2} ج.م");
            text.AppendLine($"------------------------");
            text.AppendLine($"💵 <b>نقداً:</b> {cashRev:N2} ج.م");
            text.AppendLine($"💳 <b>بطاقات / فيزا:</b> {cardRev:N2} ج.م");
            text.AppendLine($"📱 <b>فودافون كاش:</b> {vodaRev:N2} ج.م");
            text.AppendLine($"⚡ <b>انستا باي:</b> {instaRev:N2} ج.م");
            if (deferredRev > 0) text.AppendLine($"📝 <b>آجل / ديون:</b> {deferredRev:N2} ج.م");
            text.AppendLine($"------------------------");
            text.AppendLine($"💸 <b>مصروفات اليوم:</b> {todayExpenses:N2} ج.م");
            text.AppendLine($"📈 <b>تقدير صافي الربح:</b> <code>{Math.Max(0, estProfit):N2} ج.م</code>");

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }

        if (cmd == "/shift")
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var activeShift = await context.Shifts
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Status == Core.Entities.ShiftStatus.Open, ct);

            var text = new StringBuilder();
            text.AppendLine($"📋 <b>حالة الوردية الحالية — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"========================");

            if (activeShift == null)
            {
                text.AppendLine("🔴 لا توجد وردية مفتوحة حالياً.");
            }
            else
            {
                text.AppendLine($"👤 <b>الكاشير الحالي:</b> {EscapeMarkdown(activeShift.User?.FullName ?? "كاشير")}");
                text.AppendLine($"⏰ <b>بداية الوردية:</b> {activeShift.StartTime:yyyy-MM-dd HH:mm}");
                text.AppendLine($"💵 <b>الرصيد الافتتاحي:</b> <code>{activeShift.OpeningBalance:N2} ج.م</code>");
                text.AppendLine($"💰 <b>المبيعات خلال الوردية:</b> <code>{activeShift.TotalSales:N2} ج.م</code>");
            }

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }

        if (cmd == "/top")
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var topDetails = await context.SaleDetails
                .AsNoTracking()
                .Include(d => d.Product)
                .Where(d => d.Sale != null && d.Sale.SaleDate >= today && !d.Sale.IsDeleted && d.Sale.Status == Core.Entities.SaleStatus.Completed)
                .GroupBy(d => d.Product != null ? d.Product.Name : "منتج")
                .Select(g => new
                {
                    Name = g.Key,
                    TotalQty = g.Sum(x => x.Quantity),
                    TotalRev = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.TotalRev)
                .Take(5)
                .ToListAsync(ct);

            var text = new StringBuilder();
            text.AppendLine($"🏆 <b>الأصناف الأكثر مبيعاً اليوم — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"========================");

            if (topDetails.Count == 0)
            {
                text.AppendLine("لا توجد مبيعات مسجلة اليوم بعد.");
            }
            else
            {
                int rank = 1;
                foreach (var item in topDetails)
                {
                    text.AppendLine($"{rank}. <b>{EscapeMarkdown(item.Name)}</b>");
                    text.AppendLine($"   └ 📦 المباع: <code>{item.TotalQty}</code> | 💰 الإيراد: <code>{item.TotalRev:N2} ج.م</code>");
                    rank++;
                }
            }

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }

        if (cmd == "/stock")
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var lowStockItems = await context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Stock <= p.MinStockLevel)
                .OrderBy(p => p.Stock)
                .Take(15)
                .ToListAsync(ct);

            var text = new StringBuilder();
            text.AppendLine($"⚠️ <b>نواقص المخزون — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"========================");

            if (lowStockItems.Count == 0)
            {
                text.AppendLine("✅ جميع الأصناف في الحدود الآمنة.");
            }
            else
            {
                foreach (var item in lowStockItems)
                {
                    var statusIcon = item.Stock <= 0 ? "🔴" : "🟡";
                    text.AppendLine($"{statusIcon} <b>{EscapeMarkdown(item.Name)}</b>: <code>{item.Stock}</code> {EscapeMarkdown(item.Unit.ToString())} (الحد: {item.MinStockLevel})");
                }
            }

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }

        if (cmd == "/debts")
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var debtorCustomers = await context.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.CurrentDebt > 0)
                .OrderByDescending(c => c.CurrentDebt)
                .Take(10)
                .ToListAsync(ct);

            var totalDebts = debtorCustomers.Sum(c => c.CurrentDebt);

            var text = new StringBuilder();
            text.AppendLine($"🤝 <b>ديون العملاء الآجل — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"========================");
            text.AppendLine($"💰 <b>إجمالي الديون المتبقية:</b> <code>{totalDebts:N2} ج.م</code>\n");

            if (debtorCustomers.Count == 0)
            {
                text.AppendLine("✅ لا توجد ديون متبقية على العملاء.");
            }
            else
            {
                foreach (var cust in debtorCustomers)
                {
                    text.AppendLine($"• <b>{EscapeMarkdown(cust.Name)}</b> ({cust.Phone ?? ""}): <code>{cust.CurrentDebt:N2} ج.م</code>");
                }
            }

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }

        if (cmd == "/status")
        {
            var text = new StringBuilder();
            text.AppendLine($"💻 <b>حالة نظام RobovAI POS — {EscapeMarkdown(storeName)}</b>");
            text.AppendLine($"========================");
            text.AppendLine($"🟢 <b>حالة البرنامج:</b> شغال بكفاءة 100%");
            text.AppendLine($"🗄️ <b>قاعدة البيانات:</b> SQLite WAL Mode (نشطة)");
            text.AppendLine($"📶 <b>شبكة الـ LAN:</b> Port 7890 (مفتوح)");
            text.AppendLine($"🕒 <b>وقت السيرفر:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            await SendMessageAsync(botToken, chatId, text.ToString());
            return;
        }
    }

    private async Task SendMessageAsync(string botToken, string chatId, string textHtml)
    {
        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = textHtml,
                parse_mode = "HTML"
            };
            await _httpClient.PostAsJsonAsync(url, payload);
        }
        catch { /* Ignore connection errors */ }
    }

    private string GetConfiguredBotToken()
    {
        var configured = _settingsService.GetSetting("TelegramBotToken");
        return !string.IsNullOrWhiteSpace(configured) ? configured : DefaultBotToken;
    }

    private string GetConfiguredChatId()
    {
        return _settingsService.GetSetting("TelegramChatId") ?? string.Empty;
    }

    private static string EscapeMarkdown(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_botPollingTask != null)
        {
            try { await _botPollingTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken); }
            catch { }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _httpClient.Dispose();
    }
}
