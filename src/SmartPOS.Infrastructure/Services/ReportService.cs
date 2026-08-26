using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Professional Invoice / Report Generation Service.
/// Uses QuestPDF for all PDF outputs — A4, thermal receipt PDF, and supplier orders.
/// </summary>
public class ReportService : IReportService
{
    public ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ─── Helper style ─────────────────────────────────────────────────────────────
    private static void HeaderCell(IContainer c, string text)
    {
        c.Background("#1E3A5F").Padding(6)
         .Text(text).Bold().FontSize(10).FontColor(Colors.White).AlignCenter();
    }

    // ─── Sales Report ───────────────────────────────────────────────────────────

    public Task<byte[]> GenerateSalesReportPdfAsync(DateTime startDate, DateTime endDate)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                SetupA4Page(page);
                page.Header().Element(BuildReportHeader("تقرير المبيعات",
                    $"من {startDate:dd/MM/yyyy} إلى {endDate:dd/MM/yyyy}"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(40);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(c => HeaderCell(c, "#"));
                            h.Cell().Element(c => HeaderCell(c, "رقم الفاتورة"));
                            h.Cell().Element(c => HeaderCell(c, "التاريخ"));
                            h.Cell().Element(c => HeaderCell(c, "طريقة الدفع"));
                            h.Cell().Element(c => HeaderCell(c, "الإجمالي"));
                        });
                        table.Cell().Text("1").FontSize(10).AlignCenter();
                        table.Cell().Text("لا توجد بيانات").FontSize(10).Italic();
                        table.Cell().Text("--").FontSize(10);
                        table.Cell().Text("--").FontSize(10);
                        table.Cell().Text("0.00").FontSize(10);
                    });
                });
                page.Footer().Element(BuildFooter());
            });
        });
        return Task.FromResult(doc.GeneratePdf());
    }

    // ─── Inventory Report ────────────────────────────────────────────────────────

    public Task<byte[]> GenerateInventoryReportPdfAsync()
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                SetupA4Page(page);
                page.Header().Element(BuildReportHeader("تقرير المخزون",
                    $"بتاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("يُعرض في هذا التقرير مستوى المخزون الحالي لجميع الأصناف.")
                       .FontSize(11).Italic();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(40);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "اسم الصنف", "التصنيف", "سعر البيع", "المخزون", "الحالة" })
                                h.Cell().Element(c => HeaderCell(c, t));
                        });
                        table.Cell().Text("1").FontSize(10);
                        table.Cell().Text("يرجى تصدير التقرير من قسم المنتجات").FontSize(10);
                        table.Cell().Text("--").FontSize(10);
                        table.Cell().Text("--").FontSize(10);
                        table.Cell().Text("--").FontSize(10);
                        table.Cell().Text("--").FontSize(10);
                    });
                });
                page.Footer().Element(BuildFooter());
            });
        });
        return Task.FromResult(doc.GeneratePdf());
    }

    // ─── Z-Report ────────────────────────────────────────────────────────────────

    public Task<byte[]> GenerateZReportPdfAsync(DateTime date)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                SetupA4Page(page);
                page.Header().Element(BuildReportHeader(
                    "تقرير نهاية اليوم (Z-Report)",
                    $"يوم {date:dddd, dd MMMM yyyy}",
                    accentColor: "#DC2626"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(16);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => BuildSummaryBox(c, "إجمالي المبيعات", "0.00 ج.م", "#059669"));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BuildSummaryBox(c, "نقد", "0.00 ج.م", "#2563EB"));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BuildSummaryBox(c, "بطاقة", "0.00 ج.م", "#7C3AED"));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BuildSummaryBox(c, "آجل", "0.00 ج.م", "#9333EA"));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BuildSummaryBox(c, "المصروفات", "0.00 ج.م", "#D97706"));
                    });
                    col.Item().LineHorizontal(1).LineColor("#E5E7EB");
                    col.Item().Text("ملاحظة: يرجى تشغيل Z-Report من صفحة إدارة الورديات للحصول على البيانات الحقيقية.")
                       .FontSize(11).Italic();
                });
                page.Footer().Element(BuildFooter());
            });
        });
        return Task.FromResult(doc.GeneratePdf());
    }

    // ─── POS Receipt PDF ─────────────────────────────────────────────────────────

    public byte[] GeneratePosReceiptPdf(PosReceiptModel model)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).DirectionFromRightToLeft());

                // Store header
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(model.StoreName)
                       .Bold().FontSize(18).FontColor("#1E3A5F");
                    if (!string.IsNullOrEmpty(model.StoreAddress))
                        col.Item().AlignCenter().Text(model.StoreAddress).FontSize(9).FontColor("#6B7280");
                    if (!string.IsNullOrEmpty(model.StorePhone))
                        col.Item().AlignCenter().Text($"هاتف: {model.StorePhone}").FontSize(9).FontColor("#6B7280");
                    col.Item().PaddingVertical(4).LineHorizontal(1.5f).LineColor("#1E3A5F");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(6);

                    // Invoice meta
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().Text($"رقم الفاتورة: {model.InvoiceNumber}").SemiBold().FontSize(10);
                            inner.Item().Text($"التاريخ: {model.SaleDate:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#4B5563");
                            inner.Item().Text($"الكاشير: {model.CashierName}").FontSize(9).FontColor("#4B5563");
                        });
                        row.RelativeItem().AlignRight().Column(inner =>
                        {
                            inner.Item().AlignRight().Text($"طريقة الدفع: {model.PaymentMethod}").FontSize(9);
                            if (!string.IsNullOrEmpty(model.CustomerName))
                                inner.Item().AlignRight().Text($"العميل: {model.CustomerName}").FontSize(9);
                        });
                    });

                    col.Item().LineHorizontal(0.5f).LineColor("#D1D5DB");

                    // Items table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            h.Cell().Element(c => HeaderCell(c, "الصنف"));
                            h.Cell().Element(c => HeaderCell(c, "الكمية"));
                            h.Cell().Element(c => HeaderCell(c, "سعر الوحدة"));
                            h.Cell().Element(c => HeaderCell(c, "الإجمالي"));
                        });
                        foreach (var item in model.Items)
                        {
                            table.Cell().PaddingVertical(3).Text(item.Name).FontSize(10);
                            table.Cell().PaddingVertical(3).AlignCenter().Text(item.Quantity.ToString()).FontSize(10);
                            table.Cell().PaddingVertical(3).AlignCenter().Text($"{item.UnitPrice:N2}").FontSize(10);
                            table.Cell().PaddingVertical(3).AlignRight().Text($"{item.Total:N2}").FontSize(10);
                        }
                    });

                    col.Item().LineHorizontal(0.5f).LineColor("#D1D5DB");

                    // Totals
                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"الإجمالي الفرعي: {model.Subtotal:N2} ج.م").FontSize(10);
                        if (model.DiscountAmount > 0)
                            c.Item().Text($"الخصم: -{model.DiscountAmount:N2} ج.م").FontSize(10).FontColor("#DC2626");
                        if (model.TaxAmount > 0)
                            c.Item().Text($"الضريبة: {model.TaxAmount:N2} ج.م").FontSize(10);
                        c.Item().Text($"الإجمالي: {model.TotalAmount:N2} ج.م").Bold().FontSize(12).FontColor("#1E3A5F");
                        c.Item().Text($"المدفوع: {model.AmountPaid:N2} ج.م").FontSize(10);
                        c.Item().Text($"الباقي: {model.ChangeAmount:N2} ج.م").FontSize(10).FontColor("#059669");
                    });

                    col.Item().LineHorizontal(0.5f).LineColor("#D1D5DB");

                    if (!string.IsNullOrEmpty(model.FooterMessage))
                        col.Item().AlignCenter().Text(model.FooterMessage).FontSize(9).Italic().FontColor("#6B7280");
                    col.Item().AlignCenter().Text("شكراً لتعاملكم معنا 🙏").Bold().FontSize(11).FontColor("#1E3A5F");
                });

                page.Footer().AlignCenter()
                    .Text($"طُبع في: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#9CA3AF");
            });
        });
        return doc.GeneratePdf();
    }

    // ─── Purchase Order PDF ──────────────────────────────────────────────────────

    public byte[] GeneratePurchaseOrderPdf(PurchaseOrderModel model)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                SetupA4Page(page);
                page.Header().Element(BuildReportHeader(
                    "أمر توريد",
                    $"رقم الأمر: {model.OrderNumber}  |  التاريخ: {model.OrderDate:dd/MM/yyyy}",
                    accentColor: "#7C3AED"));

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Background("#F5F3FF").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("بيانات المورد").Bold().FontSize(12).FontColor("#7C3AED");
                            c.Item().Text($"الاسم: {model.SupplierName}").FontSize(10);
                            c.Item().Text($"الهاتف: {model.SupplierPhone}").FontSize(10);
                            c.Item().Text($"البريد: {model.SupplierEmail}").FontSize(10);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("حالة الأمر").Bold().FontSize(12);
                            c.Item().Text(model.Status).FontSize(11).FontColor("#7C3AED");
                        });
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(40);
                            c.RelativeColumn(3);
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        table.Header(h =>
                        {
                            foreach (var t in new[] { "#", "الصنف", "الكمية", "سعر الوحدة", "الإجمالي" })
                                h.Cell().Element(c => HeaderCell(c, t));
                        });

                        int idx = 1;
                        foreach (var item in model.Items)
                        {
                            table.Cell().Text(idx++.ToString()).AlignCenter().FontSize(10);
                            table.Cell().Text(item.ProductName).FontSize(10);
                            table.Cell().Text(item.Quantity.ToString()).AlignCenter().FontSize(10);
                            table.Cell().Text($"{item.UnitPrice:N2}").AlignCenter().FontSize(10);
                            table.Cell().Text($"{item.Total:N2}").AlignRight().FontSize(10);
                        }
                    });

                    col.Item().AlignRight().Column(c =>
                    {
                        c.Item().Text($"الإجمالي الكلي: {model.TotalAmount:N2} ج.م")
                            .Bold().FontSize(13).FontColor("#7C3AED");
                        c.Item().Text($"المدفوع: {model.PaidAmount:N2} ج.م").FontSize(11);
                        c.Item().Text($"المتبقي: {(model.TotalAmount - model.PaidAmount):N2} ج.م")
                            .FontSize(11).FontColor("#DC2626");
                    });

                    if (!string.IsNullOrEmpty(model.Notes))
                        col.Item().Background("#FFFBEB").Padding(10)
                           .Text($"ملاحظات: {model.Notes}").FontSize(10).Italic();
                });

                page.Footer().Element(BuildFooter());
            });
        });
        return doc.GeneratePdf();
    }

    // ─── Excel Export ─────────────────────────────────────────────────────────────

    public Task<bool> ExportToExcelAsync(string filePath, object data)
    {
        try
        {
            if (data is IEnumerable<object> rows)
            {
                var lines = new List<string>();
                foreach (var row in rows)
                {
                    var props = row.GetType().GetProperties();
                    if (lines.Count == 0)
                        lines.Add(string.Join(",", props.Select(p => p.Name)));
                    lines.Add(string.Join(",", props.Select(p => $"\"{p.GetValue(row)}\"")));
                }
                File.WriteAllLines(filePath, lines, System.Text.Encoding.UTF8);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    // ─── Page Setup Helpers ──────────────────────────────────────────────────────

    private static void SetupA4Page(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11).DirectionFromRightToLeft());
    }

    private static void BuildReportHeader(IContainer container, string title, string subtitle, string accentColor = "#1E3A5F")
    {
        container.Column(col =>
        {
            col.Item().Background(accentColor).Padding(14).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(title).Bold().FontSize(20).FontColor(Colors.White);
                    c.Item().Text(subtitle).FontSize(10).FontColor("#CBD5E1");
                });
                row.AutoItem().AlignRight().AlignMiddle()
                   .Text("SmartPOS").Bold().FontSize(14).FontColor("#94A3B8");
            });
            col.Item().LineHorizontal(2).LineColor(accentColor);
        });
    }

    private static Action<IContainer> BuildReportHeader(string title, string subtitle, string accentColor = "#1E3A5F")
        => c => BuildReportHeader(c, title, subtitle, accentColor);

    private static void BuildSummaryBox(IContainer container, string label, string value, string color)
    {
        container.Border(1).BorderColor("#E5E7EB").Padding(10).Column(c =>
        {
            c.Item().Text(label).FontSize(10).FontColor("#6B7280");
            c.Item().Text(value).Bold().FontSize(14).FontColor(color);
        });
    }

    private static void BuildFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"تم الطباعة: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#9CA3AF");
            row.AutoItem().Text(x =>
            {
                x.Span("صفحة ").FontSize(8).FontColor("#9CA3AF");
                x.CurrentPageNumber().FontSize(8);
                x.Span(" من ").FontSize(8).FontColor("#9CA3AF");
                x.TotalPages().FontSize(8);
            });
        });
    }

    private static Action<IContainer> BuildFooter() => c => BuildFooter(c);
}

// ─── Data Models ─────────────────────────────────────────────────────────────────

public record PosReceiptModel(
    string StoreName,
    string StoreAddress,
    string StorePhone,
    string InvoiceNumber,
    DateTime SaleDate,
    string CashierName,
    string CustomerName,
    string PaymentMethod,
    List<PosReceiptItem> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal ChangeAmount,
    string FooterMessage
);

public record PosReceiptItem(string Name, int Quantity, decimal UnitPrice, decimal Total);

public record PurchaseOrderModel(
    string OrderNumber,
    DateTime OrderDate,
    string SupplierName,
    string SupplierPhone,
    string SupplierEmail,
    string Status,
    List<PurchaseOrderItem> Items,
    decimal TotalAmount,
    decimal PaidAmount,
    string Notes
);

public record PurchaseOrderItem(string ProductName, int Quantity, decimal UnitPrice, decimal Total);
