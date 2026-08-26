namespace SmartPOS.Core.Interfaces;

/// <summary>
/// ESC/POS thermal printer service.
/// Supports 58mm (32 chars) and 80mm (48 chars) paper.
/// Language: "Arabic" | "English" | "Both"
/// </summary>
public interface IPrintingService
{
    List<string> GetAvailablePrinters();

    /// <summary>Print a sale receipt on the thermal printer.</summary>
    Task<bool> PrintReceiptAsync(string printerName, ReceiptData receiptData,
        int receiptWidthMm = 80, string language = "Both", string drawerPin = "1", bool openDrawer = true);

    /// <summary>Open cash drawer via ESC/POS kick pulse.</summary>
    bool OpenCashDrawer(string printerName, string drawerPin = "1");

    /// <summary>Legacy overload.</summary>
    bool OpenCashDrawer(string printerName);

    /// <summary>Print the Z-Report (shift close) on the thermal printer.</summary>
    Task<bool> PrintZReportAsync(string printerName, ZReportData reportData,
        int receiptWidthMm = 80, string language = "Both");

    /// <summary>Legacy overload.</summary>
    Task<bool> PrintZReportAsync(string printerName, ZReportData reportData);

    /// <summary>Print a self-test page to verify the printer is connected.</summary>
    bool TestPrinter(string printerName);

    /// <summary>Print arbitrary report sections (generic tabular data).</summary>
    Task<bool> PrintReportAsync(string printerName, string title, List<ReportSection> sections, int receiptWidthMm = 80);

    /// <summary>Print a simplified kitchen ticket (order details only, no prices).</summary>
    Task<bool> PrintKitchenTicketAsync(string printerName, KitchenTicketData ticketData);

    /// <summary>Print a rental session ticket (for games/canteen).</summary>
    Task<bool> PrintRentalTicketAsync(string printerName, RentalTicketData ticketData, int receiptWidthMm = 80, string language = "Arabic", string drawerPin = "1", bool openDrawer = true);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public class RentalTicketData
{
    public string StoreName { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string DurationText { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? Footer { get; set; }
}

public class ReceiptData
{
    public string StoreName    { get; set; } = string.Empty;
    public string StoreAddress { get; set; } = string.Empty;
    public string Phone        { get; set; } = string.Empty;
    public string InvoiceNumber{ get; set; } = string.Empty;
    public DateTime SaleDate   { get; set; }
    public string CashierName  { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<ReceiptItem> Items { get; set; } = new();
    public decimal Subtotal      { get; set; }
    public decimal DiscountAmount{ get; set; }
    public decimal TaxAmount     { get; set; }
    public decimal TotalAmount   { get; set; }
    public decimal AmountPaid    { get; set; }
    public decimal ChangeAmount  { get; set; }
    public string PaymentMethod  { get; set; } = string.Empty;
    public string? Footer        { get; set; }
}

public class ReceiptItem
{
    public string  Name      { get; set; } = string.Empty;
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total     { get; set; }
}

public class ZReportData
{
    public DateTime ReportDate      { get; set; }
    public string   CashierName     { get; set; } = string.Empty;
    public int      TotalTransactions { get; set; }
    public decimal  TotalSales      { get; set; }
    public decimal  TotalCash       { get; set; }
    public decimal  TotalCard       { get; set; }
    public decimal  TotalVodafoneCash { get; set; }
    public decimal  TotalInstaPay   { get; set; }
    public decimal  TotalDeferred   { get; set; }
    public decimal  TotalExpenses   { get; set; }
    public decimal  NetProfit       { get; set; }
    public decimal  OpeningBalance  { get; set; }
    public decimal  ClosingBalance  { get; set; }
    /// <summary>Positive = surplus, negative = shortage.</summary>
    public decimal  BlindDifference { get; set; }
}

public class ReportSection
{
    public string Title { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = new();
    public List<ReportTableColumn> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public class ReportTableColumn
{
    public string Header { get; set; } = string.Empty;
    public int    Width  { get; set; }
}

public class KitchenTicketData
{
    public string OrderNumber  { get; set; } = string.Empty;
    public string OrderType    { get; set; } = string.Empty;
    public string TableName    { get; set; } = string.Empty;
    public string CashierName  { get; set; } = string.Empty;
    public DateTime OrderTime  { get; set; } = DateTime.Now;
    public List<KitchenTicketItem> Items { get; set; } = new();
    public string? Notes       { get; set; }
}

public class KitchenTicketItem
{
    public string ProductName { get; set; } = string.Empty;
    public int    Quantity    { get; set; }
    public string? Notes      { get; set; }
}
