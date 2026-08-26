using SmartPOS.Core.Interfaces;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// ESC/POS Thermal Printer Service.
/// This version leverages Windows GDI+ (PrintDocument) for printing to perfectly support 
/// Arabic reshaping, fonts, and Right-to-Left (RTL) alignment on all generic thermal printers (e.g. XPrinter).
/// Cash drawer is driven via raw ESC/POS kick pulse (pin 2 or pin 5).
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class PrintingService : IPrintingService
{
    private readonly ISettingsService _settings;

    public PrintingService(ISettingsService settings)
    {
        _settings = settings;
    }
    // ── ESC/POS command tables (only used for Cash Drawer & Cut now) ──────────
    private static class Cmd
    {
        public static readonly byte[] INIT         = { 0x1B, 0x40 };
        public static readonly byte[] CUT          = { 0x1D, 0x56, 0x01 }; // partial cut
        // Cash drawer – pin 2 (most common)
        public static readonly byte[] DRAWER_PIN2  = { 0x1B, 0x70, 0x00, 0x19, 0xFA };
        // Cash drawer – pin 5 (some APG drawers)
        public static readonly byte[] DRAWER_PIN5  = { 0x1B, 0x70, 0x01, 0x19, 0xFA };
    }

    // ── Win32 printer API ─────────────────────────────────────────────────────
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string n, out IntPtr h, IntPtr d);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr h);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr h, int lv, ref DOC_INFO_1 di);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr h);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr h);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr h);
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool WritePrinter(IntPtr h, IntPtr p, int c, out int w);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DOC_INFO_1
    {
        public string  pDocName;
        public string? pOutputFile;
        public string? pDataType;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public List<string> GetAvailablePrinters()
        => PrinterSettings.InstalledPrinters.Cast<string>().ToList();

    private static string ColL(string text, int colWidth)
        => text.Length >= colWidth ? text[..colWidth] : text.PadRight(colWidth);

    // ── Receipt printing (GDI+ for perfect Arabic) ────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintReceiptAsync(string printerName, ReceiptData d,
        int receiptWidthMm = 80, string language = "Both", string drawerPin = "1", bool openDrawer = true)
    {
        // 1. Print Receipt Graphical Layout
        bool printed = await Task.Run(() => FallbackGdi(printerName, d, language, receiptWidthMm));

        // 2. Open Cash Drawer via Raw Commands
        if (printed && openDrawer)
        {
            OpenCashDrawer(printerName, drawerPin);
        }

        return printed;
    }

    // ── Cash drawer ───────────────────────────────────────────────────────────
    private static byte[] GetDrawerPulse(string drawerPin)
        => drawerPin == "2" ? Cmd.DRAWER_PIN5 : Cmd.DRAWER_PIN2;

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public bool OpenCashDrawer(string printerName, string drawerPin = "1")
    {
        try
        {
            var buf = new List<byte>();
            buf.AddRange(Cmd.INIT); // Ensures printer buffer is ready to receive hardware commands
            var pulse = GetDrawerPulse(drawerPin);
            buf.AddRange(pulse);
            // Send a second pulse after 100ms for reliability using the same selected pin.
            buf.AddRange(pulse);
            return SendRaw(printerName, "CashDrawer", buf.ToArray());
        }
        catch { return false; }
    }

    public bool OpenCashDrawer(string printerName)
        => OpenCashDrawer(printerName, "1");

    // ── Printer test ──────────────────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public bool TestPrinter(string printerName)
    {
        try
        {
            // Simple raw test just to verify raw pipeline works, but we also do a GDI test
            var pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = printerName;
            pd.DocumentName = "PrinterTest";
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics!;
                var f = new Font("Tahoma", 12, FontStyle.Bold);
                var fmtCenter = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("اختبار الطابعة - Printer Test", f, Brushes.Black, new RectangleF(10, 10, e.PageBounds.Width - 20, 50), fmtCenter);
                g.DrawString($"Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", new Font("Tahoma", 10), Brushes.Black, new RectangleF(10, 50, e.PageBounds.Width - 20, 30), fmtCenter);
            };
            pd.Print();
            
            // Raw Cut Command
            var buf = new List<byte>();
            buf.AddRange(Cmd.INIT);
            buf.AddRange(Cmd.CUT);
            SendRaw(printerName, "Cut", buf.ToArray());
            return true;
        }
        catch { return false; }
    }

    // ── Z-Report (shift close) ────────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintZReportAsync(string printerName, ZReportData r,
        int receiptWidthMm = 80, string language = "Both")
    {
        return await Task.Run(() =>
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                pd.DocumentName = $"ZReport_{r.ReportDate:yyyyMMdd}";
                
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics!;
                    bool is58mm = receiptWidthMm <= 58;
                    int offset = _settings.ReceiptFontSizeOffset;

                    var fontNormal = new Font("Tahoma", Math.Max(5, (is58mm ? 7 : 9) + offset));
                    var fontBold = new Font("Tahoma", Math.Max(6, (is58mm ? 8 : 10) + offset), FontStyle.Bold);
                    var fontTitle = new Font("Tahoma", Math.Max(8, (is58mm ? 10 : 14) + offset), FontStyle.Bold);
                    
                    float margin = is58mm ? 2 : 10;
                    float width = e.PageBounds.Width - (margin * 2);
                    if (width < 100) width = is58mm ? 180 : 280;

                    float y = 10;
                    bool ar = language != "English";

                    var formatCenter = new StringFormat { Alignment = StringAlignment.Center };
                    var formatLabel = new StringFormat { Alignment = StringAlignment.Near };
                    var formatValue = new StringFormat { Alignment = StringAlignment.Far };

                    if (ar)
                    {
                        formatCenter.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                        formatLabel.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                        formatValue.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                    }

                    void DrawText(string text, Font f, float x, float w, StringFormat fmt)
                    {
                        var sz = g.MeasureString(text, f, (int)w, fmt);
                        g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, sz.Height), fmt);
                    }
                    void NewLine(Font f) { y += f.GetHeight(g) + 2; }

                    DrawText(ar ? "تقرير إغلاق الوردية" : "Z-REPORT", fontTitle, margin, width, formatCenter);
                    NewLine(fontTitle);
                    DrawText($"{r.ReportDate:dd/MM/yyyy HH:mm}", fontNormal, margin, width, formatCenter);
                    NewLine(fontNormal);
                    
                    y += 10; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                    void DrawRow(string label, string val, Font f)
                    {
                        DrawText(label, f, margin, width, formatLabel);
                        DrawText(val, f, margin, width, formatValue);
                        NewLine(f);
                    }

                    DrawRow(ar ? "الكاشير:" : "Cashier:", r.CashierName, fontNormal);
                    y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                    DrawRow(ar ? "المعاملات:" : "Transactions:", r.TotalTransactions.ToString(), fontNormal);
                    DrawRow(ar ? "إجمالي المبيعات:" : "Total Sales:", r.TotalSales.ToString("N2"), fontNormal);
                    DrawRow(ar ? "نقداً:" : "Cash:", r.TotalCash.ToString("N2"), fontNormal);
                    DrawRow(ar ? "بطاقة:" : "Card:", r.TotalCard.ToString("N2"), fontNormal);
                    DrawRow(ar ? "فودافون كاش:" : "Vodafone Cash:", r.TotalVodafoneCash.ToString("N2"), fontNormal);
                    DrawRow(ar ? "انستا باي:" : "InstaPay:", r.TotalInstaPay.ToString("N2"), fontNormal);
                    DrawRow(ar ? "آجل:" : "Deferred:", r.TotalDeferred.ToString("N2"), fontNormal);
                    
                    y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;
                    DrawRow(ar ? "المصروفات:" : "Expenses:", r.TotalExpenses.ToString("N2"), fontNormal);
                    
                    y += 5;
                    DrawRow(ar ? "صافي الربح:" : "Net Profit:", r.NetProfit.ToString("N2"), fontBold);
                    
                    if (r.ClosingBalance > 0)
                    {
                        y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;
                        DrawRow(ar ? "رصيد الافتتاح:" : "Opening Bal.:", r.OpeningBalance.ToString("N2"), fontNormal);
                        DrawRow(ar ? "رصيد الإغلاق:" : "Closing Bal.:",  r.ClosingBalance.ToString("N2"), fontNormal);
                        
                        if (r.BlindDifference != 0)
                        {
                            var diff = Math.Abs(r.BlindDifference).ToString("N2");
                            var labelAr = r.BlindDifference > 0 ? $"الزيادة: {diff}" : $"العجز: {diff}";
                            var labelEn = r.BlindDifference > 0 ? $"Surplus: {diff}" : $"Short: {diff}";
                            DrawRow(ar ? "الفرق:" : "Diff:", ar ? labelAr : labelEn, fontBold);
                        }
                    }

                    y += 10; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;
                    DrawText($"طُبع: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", fontNormal, margin, width, formatCenter);
                };
                pd.Print();
                CutPaper(printerName);
                return true;
            }
            catch { return false; }
        });
    }

    public Task<bool> PrintZReportAsync(string printerName, ZReportData reportData)
        => PrintZReportAsync(printerName, reportData, 80, "Both");

    // ── General report sections ───────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintReportAsync(string printerName, string title, List<ReportSection> sections, int receiptWidthMm = 80)
    {
        return await Task.Run(() =>
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                pd.DocumentName = title;
                
                pd.PrintPage += (s, e) =>
                {
                    var g = e.Graphics!;
                    bool is58mm = receiptWidthMm <= 58;
                    int offset = _settings.ReceiptFontSizeOffset;

                    var fontNormal = new Font("Tahoma", Math.Max(5, (is58mm ? 7 : 9) + offset));
                    var fontBold = new Font("Tahoma", Math.Max(6, (is58mm ? 8 : 10) + offset), FontStyle.Bold);
                    var fontTitle = new Font("Tahoma", Math.Max(8, (is58mm ? 10 : 12) + offset), FontStyle.Bold);
                    var fontMono = new Font("Courier New", Math.Max(5, (is58mm ? 7 : 9) + offset)); // For tabular data
                    
                    float margin = is58mm ? 2 : 10;
                    float width = e.PageBounds.Width - (margin * 2);
                    if (width < 100) width = is58mm ? 180 : 280;

                    float y = 10;

                    var formatRtl = new StringFormat { FormatFlags = StringFormatFlags.DirectionRightToLeft, Alignment = StringAlignment.Near };
                    var formatCenter = new StringFormat { Alignment = StringAlignment.Center };
                    
                    void Draw(string text, Font f, StringFormat fmt)
                    {
                        var sz = g.MeasureString(text, f, (int)width, fmt);
                        g.DrawString(text, f, Brushes.Black, new RectangleF(margin, y, width, sz.Height), fmt);
                        y += sz.Height + 2;
                    }

                    Draw(title, fontTitle, formatCenter);
                    Draw($"{DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, formatCenter);
                    y += 10;

                    foreach (var sec in sections)
                    {
                        if (!string.IsNullOrEmpty(sec.Title)) {
                            Draw(sec.Title, fontBold, formatRtl);
                            y += 2;
                            g.DrawLine(Pens.Black, margin, y, margin + width, y);
                            y += 5;
                        }
                        foreach (var line in sec.Lines) {
                            Draw(line, fontNormal, formatRtl);
                        }
                        
                        if (sec.Columns.Any())
                        {
                            string header = string.Concat(sec.Columns.Select(c => ColL(c.Header, c.Width)));
                            Draw(header, fontMono, formatRtl);
                            y += 2; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;
                            
                            foreach(var row in sec.Rows) {
                                var rowLine = string.Concat(row.Select((cell, i) => i < sec.Columns.Count ? ColL(cell, sec.Columns[i].Width) : ""));
                                Draw(rowLine, fontMono, formatRtl);
                            }
                        }
                        y += 10;
                    }
                };
                pd.Print();
                CutPaper(printerName);
                return true;
            }
            catch { return false; }
        });
    }

    // ── Win32 RAW send (used for Kick pulse / Cut) ────────────────────────────
    public bool CutPaper(string printerName)
    {
        var buf = new List<byte>();
        buf.AddRange(Cmd.INIT);
        buf.AddRange(Cmd.CUT);
        return SendRaw(printerName, "CutPaper", buf.ToArray());
    }

    private bool SendRaw(string printerName, string docName, byte[] data)
    {
        IntPtr hPrinter = IntPtr.Zero;
        try
        {
            var di = new DOC_INFO_1 { pDocName = docName ?? "SmartPOS", pDataType = "RAW" };
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero)) return false;
            if (!StartDocPrinter(hPrinter, 1, ref di)) return false;
            if (!StartPagePrinter(hPrinter)) return false;

            var ptr = Marshal.AllocCoTaskMem(data.Length);
            bool ok;
            try { Marshal.Copy(data, 0, ptr, data.Length); ok = WritePrinter(hPrinter, ptr, data.Length, out _); }
            finally { Marshal.FreeCoTaskMem(ptr); }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            return ok;
        }
        catch { return false; }
        finally { if (hPrinter != IntPtr.Zero) ClosePrinter(hPrinter); }
    }

    // ── GDI+ Receipt Generation ───────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private bool FallbackGdi(string printerName, ReceiptData d, string language, int receiptWidthMm)
    {
        try
        {
            var pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = printerName;
            pd.DocumentName = $"فاتورة_{d.InvoiceNumber}";
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0); // Remove large top margin
            pd.PrintPage += (_, e) =>
            {
                var g = e.Graphics!;
                bool is58mm = receiptWidthMm <= 58;
                int offset = _settings.ReceiptFontSizeOffset;
                
                var fontNormal = new Font("Tahoma", Math.Max(5, (is58mm ? 7 : 9) + offset));
                var fontBold = new Font("Tahoma", Math.Max(6, (is58mm ? 8 : 10) + offset), FontStyle.Bold);
                var fontHeader = new Font("Tahoma", Math.Max(8, (is58mm ? 10 : 14) + offset), FontStyle.Bold);
                
                float margin = is58mm ? 2 : 10;
                float width = e.PageBounds.Width - (margin * 2);
                if (width < 100) width = is58mm ? 180 : 280; // Safety fallback
                
                float y = 2; // Reduced starting Y
                bool ar = language != "English";

                var formatCenter = new StringFormat { Alignment = StringAlignment.Center };
                var formatLabel = new StringFormat { Alignment = StringAlignment.Near };
                var formatValue = new StringFormat { Alignment = StringAlignment.Far };
                var formatTable = new StringFormat { Alignment = StringAlignment.Near };

                if (ar)
                {
                    formatCenter.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                    formatLabel.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                    formatValue.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                    formatTable.FormatFlags = StringFormatFlags.DirectionRightToLeft;
                }

                void DrawText(string text, Font f, float x, float w, StringFormat fmt)
                {
                    var sz = g.MeasureString(text, f, (int)w, fmt);
                    g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, sz.Height), fmt);
                }
                void NewLine(Font f) { y += f.GetHeight(g); } // Removed the +2 spacing for a tighter fit

                // Logo
                if (!string.IsNullOrEmpty(_settings.StoreLogoPath) && System.IO.File.Exists(_settings.StoreLogoPath))
                {
                    try
                    {
                        using var img = Image.FromFile(_settings.StoreLogoPath);
                        float imgMaxWidth = width * 0.25f; // Max 25% of receipt width (was 50%)
                        float imgWidth = Math.Min(img.Width, imgMaxWidth);
                        float imgHeight = (imgWidth / img.Width) * img.Height;
                        float imgX = margin + (width - imgWidth) / 2;
                        g.DrawImage(img, imgX, y, imgWidth, imgHeight);
                        y += imgHeight + 5; // Reduced margin after logo
                    }
                    catch { /* Ignore image loading errors */ }
                }

                // Header
                DrawText(d.StoreName, fontHeader, margin, width, formatCenter);
                NewLine(fontHeader);
                if (!string.IsNullOrEmpty(d.StoreAddress)) { DrawText(d.StoreAddress, fontNormal, margin, width, formatCenter); NewLine(fontNormal); }
                if (!string.IsNullOrEmpty(d.Phone)) { DrawText((ar ? "هاتف: " : "Tel: ") + d.Phone, fontNormal, margin, width, formatCenter); NewLine(fontNormal); }
                if (!string.IsNullOrEmpty(_settings.StoreTaxNumber)) { DrawText((ar ? "الرقم الضريبي: " : "Tax ID: ") + _settings.StoreTaxNumber, fontNormal, margin, width, formatCenter); NewLine(fontNormal); }
                
                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 3;

                // Meta
                void DrawMeta(string label, string val)
                {
                    // Draw both in the same row rectangle, they will align to opposite sides thanks to Near/Far
                    DrawText(label, fontNormal, margin, width, formatLabel);
                    DrawText(val, fontNormal, margin, width, formatValue);
                    NewLine(fontNormal);
                }
                DrawMeta(ar ? "رقم الفاتورة:" : "Invoice No:", d.InvoiceNumber);
                DrawMeta(ar ? "التاريخ:" : "Date:", d.SaleDate.ToString("dd/MM/yyyy HH:mm"));
                DrawMeta(ar ? "الكاشير:" : "Cashier:", d.CashierName);
                if (!string.IsNullOrEmpty(d.CustomerName)) DrawMeta(ar ? "العميل:" : "Customer:", d.CustomerName);
                
                y += 3; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 3;

                // Items Header Layout (Proportional RTL/LTR)
                float wName = width * 0.40f;
                float wQty = width * 0.15f;
                float wPrice = width * 0.22f;
                float wTotal = width * 0.23f;

                float xTotal, xPrice, xQty, xName;
                if (ar)
                {
                    xTotal = margin;
                    xPrice = xTotal + wTotal;
                    xQty = xPrice + wPrice;
                    xName = xQty + wQty;
                }
                else
                {
                    xName = margin;
                    xQty = xName + wName;
                    xPrice = xQty + wQty;
                    xTotal = xPrice + wPrice;
                }

                float DrawTableRow(string n, string q, string p, string t, Font f)
                {
                    var szN = g.MeasureString(n, f, (int)wName, formatTable);
                    var szQ = g.MeasureString(q, f, (int)wQty, formatTable);
                    var szP = g.MeasureString(p, f, (int)wPrice, formatTable);
                    var szT = g.MeasureString(t, f, (int)wTotal, formatTable);
                    float maxH = Math.Max(szN.Height, Math.Max(szQ.Height, Math.Max(szP.Height, szT.Height)));

                    g.DrawString(n, f, Brushes.Black, new RectangleF(xName, y, wName, maxH), formatTable);
                    g.DrawString(q, f, Brushes.Black, new RectangleF(xQty, y, wQty, maxH), formatTable);
                    g.DrawString(p, f, Brushes.Black, new RectangleF(xPrice, y, wPrice, maxH), formatTable);
                    g.DrawString(t, f, Brushes.Black, new RectangleF(xTotal, y, wTotal, maxH), formatTable);
                    return maxH;
                }

                y += DrawTableRow(ar ? "الصنف" : "Item", ar ? "كمية" : "Qty", ar ? "سعر" : "Price", ar ? "إجمالي" : "Total", fontBold) + 2;
                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                // Items
                foreach(var item in d.Items)
                {
                    y += DrawTableRow(item.Name, item.Quantity.ToString(), item.UnitPrice.ToString("N2"), item.Total.ToString("N2"), fontNormal) + 2;
                }

                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                // Totals
                DrawMeta(ar ? "المجموع:" : "Subtotal:", d.Subtotal.ToString("N2"));
                if (d.DiscountAmount > 0) DrawMeta(ar ? "الخصم:" : "Discount:", d.DiscountAmount.ToString("N2"));
                if (d.TaxAmount > 0) DrawMeta(ar ? "الضريبة:" : "Tax:", d.TaxAmount.ToString("N2"));
                
                y += 5;
                DrawText(ar ? "الإجمالي:" : "TOTAL:", fontBold, margin, width, formatLabel);
                DrawText(d.TotalAmount.ToString("N2"), fontBold, margin, width, formatValue);
                NewLine(fontBold);
                y += 5;

                DrawMeta(ar ? "المدفوع:" : "Paid:", d.AmountPaid.ToString("N2"));
                DrawMeta(ar ? "الباقي:" : "Change:", d.ChangeAmount.ToString("N2"));
                
                string pmAr = d.PaymentMethod switch { "Cash" => "نقداً", "Card" => "بطاقة", "Mixed" => "نقد + بطاقة", "Wallet" => "محفظة", _ => d.PaymentMethod };
                DrawMeta(ar ? "طريقة الدفع:" : "Payment:", ar ? pmAr : d.PaymentMethod);

                y += 10; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                if (!string.IsNullOrEmpty(d.Footer))
                {
                    var sz = g.MeasureString(d.Footer, fontBold, (int)width, formatCenter);
                    g.DrawString(d.Footer, fontBold, Brushes.Black, new RectangleF(margin, y, width, sz.Height), formatCenter);
                    y += sz.Height + 10;
                }
            };
            pd.Print();
            return true;
        }
        catch { return false; }
    }

    // ── Kitchen Ticket ────────────────────────────────────────────────────────
    public Task<bool> PrintKitchenTicketAsync(string printerName, KitchenTicketData ticket)
    {
        return Task.Run(() =>
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = printerName;
                float margin = 15f;
                float width  = 200f;

                using var fontLarge = new Font("Courier New", 14, FontStyle.Bold);
                using var fontNorm  = new Font("Courier New", 11, FontStyle.Regular);
                using var fontBold  = new Font("Courier New", 11, FontStyle.Bold);
                var fmtC = new StringFormat { Alignment = StringAlignment.Center };
                var fmtR = new StringFormat { Alignment = StringAlignment.Far };

                pd.PrintPage += (_, e) =>
                {
                    if (e.Graphics == null) return;
                    var g = e.Graphics;
                    float y = margin;

                    void Line(string text, Font f, StringFormat fmt)
                    {
                        var sz = g.MeasureString(text, f, (int)width, fmt);
                        g.DrawString(text, f, Brushes.Black, new RectangleF(margin, y, width, sz.Height), fmt);
                        y += sz.Height + 2;
                    }

                    Line("=== طلب مطبخ ===", fontLarge, fmtC);
                    Line($"#{ticket.OrderNumber}", fontBold, fmtC);
                    Line($"{ticket.OrderType}  |  {ticket.TableName}", fontNorm, fmtC);
                    Line($"الكاشير: {ticket.CashierName}", fontNorm, fmtC);
                    Line($"الوقت: {ticket.OrderTime:HH:mm:ss}", fontNorm, fmtC);
                    Line(new string('-', 28), fontNorm, fmtC);

                    foreach (var item in ticket.Items)
                    {
                        Line($"  {item.Quantity}x  {item.ProductName}", fontBold, new StringFormat());
                        if (!string.IsNullOrWhiteSpace(item.Notes))
                            Line($"      * {item.Notes}", fontNorm, new StringFormat());
                    }

                    Line(new string('=', 28), fontNorm, fmtC);
                    if (!string.IsNullOrWhiteSpace(ticket.Notes))
                        Line($"ملاحظة: {ticket.Notes}", fontBold, new StringFormat());
                };

                pd.Print();
                return true;
            }
            catch { return false; }
        });
    }


    // ── Rental Ticket ─────────────────────────────────────────────────────────
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public async Task<bool> PrintRentalTicketAsync(string printerName, RentalTicketData d, int receiptWidthMm = 80, string language = "Arabic", string drawerPin = "1", bool openDrawer = true)
    {
        bool printed = await Task.Run(() => FallbackGdiRental(printerName, d, language, receiptWidthMm));

        if (printed)
        {
            if (openDrawer) OpenCashDrawer(printerName, drawerPin);
            CutPaper(printerName);
        }

        return printed;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private bool FallbackGdiRental(string printerName, RentalTicketData d, string language, int receiptWidthMm)
    {
        try
        {
            var pd = new PrintDocument();
            pd.PrinterSettings.PrinterName = printerName;
            pd.DocumentName = $"Rental_{d.InvoiceNumber}";
            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics!;
                bool is58mm = receiptWidthMm <= 58;
                int offset = _settings.ReceiptFontSizeOffset;

                var fontNormal = new Font("Tahoma", Math.Max(5, (is58mm ? 7 : 9) + offset));
                var fontBold = new Font("Tahoma", Math.Max(6, (is58mm ? 8 : 10) + offset), FontStyle.Bold);
                var fontTitle = new Font("Tahoma", Math.Max(8, (is58mm ? 12 : 16) + offset), FontStyle.Bold);
                var fontHuge = new Font("Tahoma", Math.Max(10, (is58mm ? 16 : 22) + offset), FontStyle.Bold);

                float margin = is58mm ? 2 : 10;
                float width = e.PageBounds.Width - (margin * 2);
                if (width < 100) width = is58mm ? 180 : 280;

                float y = 5;
                bool ar = language != "English";

                var formatCenter = new StringFormat { Alignment = StringAlignment.Center };
                if (ar) formatCenter.FormatFlags = StringFormatFlags.DirectionRightToLeft;

                void DrawText(string text, Font f, float x, float w, StringFormat fmt)
                {
                    var sz = g.MeasureString(text, f, (int)w, fmt);
                    g.DrawString(text, f, Brushes.Black, new RectangleF(x, y, w, sz.Height), fmt);
                }
                void NewLine(Font f) { y += f.GetHeight(g) + 2; }

                // Header
                DrawText(d.StoreName, fontTitle, margin, width, formatCenter);
                NewLine(fontTitle);
                
                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                DrawText(ar ? "تذكرة لعب / حجز" : "Rental Ticket", fontBold, margin, width, formatCenter);
                NewLine(fontBold);
                
                y += 5;

                // Device Name (HUGE)
                DrawText(d.DeviceName, fontHuge, margin, width, formatCenter);
                NewLine(fontHuge);
                
                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                // Duration & Time
                DrawText(ar ? $"المدة: {d.DurationText}" : $"Duration: {d.DurationText}", fontTitle, margin, width, formatCenter);
                NewLine(fontTitle);
                
                DrawText(ar ? $"البداية: {d.StartTime:hh:mm tt}" : $"Start: {d.StartTime:hh:mm tt}", fontBold, margin, width, formatCenter);
                NewLine(fontBold);
                
                if (d.EndTime.HasValue)
                {
                    DrawText(ar ? $"النهاية: {d.EndTime.Value:hh:mm tt}" : $"End: {d.EndTime.Value:hh:mm tt}", fontBold, margin, width, formatCenter);
                    NewLine(fontBold);
                }

                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                // Financials
                DrawText(ar ? $"المبلغ: {d.TotalAmount:N2} ج.م" : $"Amount: {d.TotalAmount:N2} EGP", fontTitle, margin, width, formatCenter);
                NewLine(fontTitle);
                
                if (!string.IsNullOrEmpty(d.CustomerName))
                {
                    DrawText(ar ? $"العميل: {d.CustomerName}" : $"Customer: {d.CustomerName}", fontBold, margin, width, formatCenter);
                    NewLine(fontBold);
                }

                y += 5; g.DrawLine(Pens.Black, margin, y, margin + width, y); y += 5;

                // Meta
                DrawText($"#{d.InvoiceNumber}", fontNormal, margin, width, formatCenter);
                NewLine(fontNormal);
                DrawText(d.StartTime.ToString("dd/MM/yyyy"), fontNormal, margin, width, formatCenter);
                NewLine(fontNormal);

                if (!string.IsNullOrEmpty(d.Footer))
                {
                    y += 10;
                    DrawText(d.Footer, fontBold, margin, width, formatCenter);
                }
            };
            pd.Print();
            return true;
        }
        catch { return false; }
    }
}

