using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using ClosedXML.Excel;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// Refactored to use IDbContextFactory for v5.1
/// </summary>
public partial class ProductsViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IAuthorizationService _authService;
    private readonly User _currentUser;

    [ObservableProperty] private ObservableCollection<Product>  _products         = new();
    [ObservableProperty] private ObservableCollection<Product>  _filteredProducts = new();
    [ObservableProperty] private ObservableCollection<Category> _categories       = new();

    public List<string> Units { get; } = Enum.GetNames(typeof(UnitType)).ToList();

    // ─── Filters ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _searchText            = string.Empty;
    [ObservableProperty] private int?   _selectedCategoryFilter;

    // ─── Form fields ───────────────────────────────────────────────────────
    [ObservableProperty] private int?    _editingProductId;
    [ObservableProperty] private string  _formName          = string.Empty;
    [ObservableProperty] private string  _formBarcode       = string.Empty;
    [ObservableProperty] private decimal _formPurchasePrice;
    [ObservableProperty] private decimal _formSellingPrice;
    [ObservableProperty] private int     _formStock;
    [ObservableProperty] private int     _formMinStockLevel = 10;
    [ObservableProperty] private int     _formCategoryId;
    [ObservableProperty] private string  _formUnit          = UnitType.Piece.ToString();
    [ObservableProperty] private string? _formDescription;
    [ObservableProperty] private string? _formImagePath;
    [ObservableProperty] private bool    _formIsActive      = true;
    [ObservableProperty] private Product? _selectedProduct;

    public bool IsAdmin =>
        _currentUser.Role is UserRole.Admin or UserRole.Manager or UserRole.SuperAdmin;

    private readonly ISettingsService? _settingsService;

    public ProductsViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        User currentUser,
        IAuthorizationService authService,
        ISettingsService? settingsService = null)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;
        _settingsService = settingsService;

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(SmartPOS.Application.Messages.BarcodeScannedMessage message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            // If we are currently filling the form (creating or editing)
            if (EditingProductId.HasValue || !string.IsNullOrWhiteSpace(FormName) || !string.IsNullOrWhiteSpace(FormBarcode))
            {
                FormBarcode = message.Value;
            }
            else
            {
                // Otherwise, we are just browsing, so search for it
                SearchText = message.Value;
            }
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // LOAD  ← اسم الـ Command مطابق لما يستدعيه ProductsPage.xaml.cs
    // ════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task LoadProducts()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var list = await ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .ToListAsync();

            Products.SyncWith(list);
            ApplyFilter();          // يملأ FilteredProducts أيضاً
        }, "⏳ جاري تحميل المنتجات...", "✅ تم التحميل");
    }

    [RelayCommand]
    public async Task LoadCategories()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var list = await ctx.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            Categories.SyncWith(list);
        }, "جاري تحميل الأقسام...");
    }

    // ════════════════════════════════════════════════════════════════════
    // FILTER
    // ════════════════════════════════════════════════════════════════════

    partial void OnSearchTextChanged(string value)            => ApplyFilter();
    partial void OnSelectedCategoryFilterChanged(int? value)  => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<Product> source = Products;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            source = source.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCategoryFilter is > 0)
            source = source.Where(p => p.CategoryId == SelectedCategoryFilter);

        // ToList() يجمّد النتيجة قبل SyncWith — مهم جداً
        FilteredProducts.SyncWith(source.ToList());
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        SelectedCategoryFilter = null;
    }

    [RelayCommand]
    private async Task RefreshProducts()
    {
        ClearFilter();
        await LoadProducts();
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (FilteredProducts.Count == 0)
        {
            MessageBox.Show("لا يوجد بيانات لتصديرها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).DirectionFromRightToLeft());

                    page.Header().Column(col =>
                    {
                        col.Item().Background("#1E3A5F").Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("قائمة المنتجات والمخزون").Bold().FontSize(22).FontColor(Colors.White);
                            });
                            row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().Text("SmartPOS").Bold().FontSize(14).FontColor("#94A3B8");
                                c.Item().Text($"تاريخ الإصدار: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#94A3B8");
                            });
                        });
                        col.Item().LineHorizontal(2).LineColor("#1E3A5F");
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);
                        
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"إجمالي المنتجات المعروضة: {FilteredProducts.Count}").Bold().FontSize(12);
                            r.RelativeItem().AlignRight().Text($"إجمالي قيمة المخزون (بيع): {FilteredProducts.Sum(p => p.SellingPrice * p.Stock):N2} ج.م").FontSize(11);
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);
                                c.RelativeColumn(2);
                                c.RelativeColumn(4);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1.5f);
                            });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "#", "الباركود", "اسم المنتج", "التصنيف", "السعر", "المخزون" })
                                    h.Cell().Background("#1E3A5F").Padding(6).Text(t).Bold().FontSize(10).FontColor(Colors.White).AlignCenter();
                            });

                            int idx = 1;
                            foreach (var p in FilteredProducts)
                            {
                                var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(5).Text(idx++.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(p.Barcode ?? "").AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(p.Name ?? "").FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(p.Category?.Name ?? "").AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text($"{p.SellingPrice:N2}").AlignCenter().FontSize(10).Bold();
                                table.Cell().Background(bg).Padding(5).Text(p.Stock.ToString()).AlignCenter().FontSize(10).FontColor(p.Stock <= p.MinStockLevel ? "#DC2626" : "#000000");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"طُبع بواسطة {(_currentUser?.FullName ?? "النظام")} - صفحة ").FontSize(8).FontColor("#9CA3AF");
                });
            }).GeneratePdf();

            var dlg = new SaveFileDialog
            {
                Title = "حفظ قائمة المنتجات",
                FileName = $"المنتجات-{DateTime.Now:yyyyMMdd}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF Files|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdfBytes);
                MessageBox.Show($"✅ تم تصدير القائمة بنجاح:\n{dlg.FileName}", "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء تصدير PDF:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (FilteredProducts.Count == 0)
        {
            MessageBox.Show("لا توجد بيانات لتصديرها.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var defaultFolder = _settingsService?.DefaultExportFolder;
        if (!string.IsNullOrWhiteSpace(defaultFolder) && !Directory.Exists(defaultFolder))
        {
            try { Directory.CreateDirectory(defaultFolder); } catch { }
        }

        var dlg = new SaveFileDialog
        {
            Title = "حفظ قائمة المنتجات في إكسيل",
            FileName = $"المنتجات-{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
            InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && Directory.Exists(defaultFolder)
                ? defaultFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            DefaultExt = ".xlsx",
            Filter = "Excel Files (*.xlsx)|*.xlsx"
        };

        if (dlg.ShowDialog() == true)
        {
            await ExecuteBusyAsync(() =>
            {
                return Task.Run(() =>
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("المنتجات");
                    ws.RightToLeft = true;

                    // Headers
                    ws.Cell(1, 1).Value = "الباركود";
                    ws.Cell(1, 2).Value = "اسم المنتج";
                    ws.Cell(1, 3).Value = "سعر الشراء";
                    ws.Cell(1, 4).Value = "سعر البيع";
                    ws.Cell(1, 5).Value = "المخزون";
                    ws.Cell(1, 6).Value = "الحد الأدنى";
                    ws.Cell(1, 7).Value = "الوحدة";
                    ws.Cell(1, 8).Value = "القسم";
                    ws.Cell(1, 9).Value = "الوصف";

                    var headerRow = ws.Row(1);
                    headerRow.Height = 28;
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
                    headerRow.Style.Font.FontColor = XLColor.White;
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    int row = 2;
                    foreach (var p in FilteredProducts)
                    {
                        ws.Cell(row, 1).Value = p.Barcode;
                        ws.Cell(row, 2).Value = p.Name;
                        ws.Cell(row, 3).Value = p.PurchasePrice;
                        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 4).Value = p.SellingPrice;
                        ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                        ws.Cell(row, 5).Value = p.Stock;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(row, 6).Value = p.MinStockLevel;
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        ws.Cell(row, 7).Value = p.Unit.ToString();
                        ws.Cell(row, 8).Value = p.Category?.Name ?? "عام";
                        ws.Cell(row, 9).Value = p.Description ?? "";

                        // Zebra striping
                        if (row % 2 == 0)
                        {
                            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        }

                        row++;
                    }

                    // Total summary footer row
                    var footerRow = ws.Row(row);
                    footerRow.Height = 24;
                    footerRow.Style.Font.Bold = true;
                    footerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    ws.Cell(row, 2).Value = $"الإجمالي ({FilteredProducts.Count} منتج)";
                    ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                    ws.Columns().AdjustToContents(12, 50);
                    wb.SaveAs(dlg.FileName);

                    // Auto open file if configured in Settings
                    if (_settingsService?.AutoOpenExportedFile == true)
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                        }
                        catch { }
                    }
                });
            }, "جاري التصدير...", $"✅ تم التصدير بنجاح:\n{dlg.FileName}");
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var defaultFolder = _settingsService?.DefaultExportFolder;
        var dlg = new OpenFileDialog
        {
            Title = "استيراد منتجات من ملف إكسيل أو CSV",
            Filter = "Excel & CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
            InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && Directory.Exists(defaultFolder)
                ? defaultFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (dlg.ShowDialog() != true) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("استيراد قائمة منتجات من ملف خارجي");
        if (!authorized) return;

        await ExecuteBusyAsync(async () =>
        {
            try
            {
                var result = await Task.Run(async () => await ImportFromFileAsync(dlg.FileName));
                if (result.skipped > 0 && !string.IsNullOrWhiteSpace(result.errorLog))
                {
                    var msg = result.summary + "\n\nهل ترغب في استعراض تقرير الملاحظات؟";
                    if (MessageBox.Show(msg, "نتيجة الاستيراد", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    {
                        MessageBox.Show(result.errorLog, "تقرير الاستيراد التفصيلي", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(result.summary, "نجاح الاستيراد", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ تعذر إتمام عملية الاستيراد:\n{ex.Message}", "خطأ في الاستيراد", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري قراءة واستيراد البيانات وفحص الأعمدة...");
    }

    public async Task<(int added, int updated, int skipped, string summary, string errorLog)> ImportFromFileAsync(string filePath)
    {
        int added = 0;
        int updated = 0;
        int skipped = 0;
        var errorLog = new System.Text.StringBuilder();

        await using var ctx = await _contextFactory.CreateDbContextAsync();

        // Load existing categories or ensure at least one default exists
        var existingCategories = await ctx.Categories.ToListAsync();
        if (existingCategories.Count == 0)
        {
            var defCat = new Category { Name = "عام", Description = "قسم افتراضي للنظام", IsActive = true, CreatedAt = DateTime.Now };
            ctx.Categories.Add(defCat);
            await ctx.SaveChangesAsync();
            existingCategories.Add(defCat);
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var duplicateAction = _settingsService?.ImportDuplicateAction ?? "UpdateStock";
        var autoCreateCategories = _settingsService?.ImportAutoCreateCategories ?? true;

        List<string[]> dataRows = new();
        string[]? headerColumns = null;

        if (ext == ".csv")
        {
            var csvLines = await File.ReadAllLinesAsync(filePath, System.Text.Encoding.UTF8);
            if (csvLines.Length == 0) throw new InvalidOperationException("ملف CSV فارغ ولا يحتوي على بيانات.");

            static string[] SplitCsv(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return Array.Empty<string>();
                var result = new List<string>();
                bool inQuotes = false;
                var cur = new System.Text.StringBuilder();
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            cur.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = !inQuotes;
                        }
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        result.Add(cur.ToString().Trim().Trim('"', '\'', ' '));
                        cur.Clear();
                    }
                    else
                    {
                        cur.Append(c);
                    }
                }
                result.Add(cur.ToString().Trim().Trim('"', '\'', ' '));
                return result.ToArray();
            }

            headerColumns = SplitCsv(csvLines[0]);
            for (int i = 1; i < csvLines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(csvLines[i]))
                    dataRows.Add(SplitCsv(csvLines[i]));
            }
        }
        else
        {
            using var wb = new XLWorkbook(filePath);
            if (wb.Worksheets.Count == 0)
                throw new InvalidOperationException("ملف الإكسيل فارغ ولا يحتوي على أي أوراق عمل.");

            var ws = wb.Worksheet(1);
            var rangeUsed = ws.RangeUsed();
            if (rangeUsed == null) throw new InvalidOperationException("ورقة العمل فارغة.");

            var firstRow = rangeUsed.Row(1);
            int lastCol = rangeUsed.ColumnCount();
            headerColumns = new string[lastCol];
            for (int c = 1; c <= lastCol; c++)
            {
                headerColumns[c - 1] = firstRow.Cell(c).Value.ToString().Trim();
            }

            var rows = rangeUsed.RowsUsed().Skip(1);
            foreach (var r in rows)
            {
                var rowArr = new string[lastCol];
                for (int c = 1; c <= lastCol; c++)
                {
                    rowArr[c - 1] = r.Cell(c).Value.ToString().Trim();
                }
                dataRows.Add(rowArr);
            }
        }

        // Smart Header Column Mapping ("تلقط منه")
        int barcodeIdx = -1, nameIdx = -1, purchaseIdx = -1, sellIdx = -1;
        int stockIdx = -1, minStockIdx = -1, categoryIdx = -1, unitIdx = -1, descIdx = -1;

        if (headerColumns != null)
        {
            for (int i = 0; i < headerColumns.Length; i++)
            {
                var h = headerColumns[i].ToLowerInvariant();
                if (barcodeIdx == -1 && (h.Contains("باركود") || h.Contains("كود") || h.Contains("barcode") || h.Contains("code") || h.Contains("upc") || h.Contains("ean") || h.Contains("sku")))
                    barcodeIdx = i;
                else if (nameIdx == -1 && (h.Contains("اسم") || h.Contains("صنف") || h.Contains("منتج") || h.Contains("name") || h.Contains("item") || h.Contains("product") || h.Contains("title")))
                    nameIdx = i;
                else if (purchaseIdx == -1 && (h.Contains("شراء") || h.Contains("تكلفة") || h.Contains("cost") || h.Contains("buy") || h.Contains("purchase")))
                    purchaseIdx = i;
                else if (sellIdx == -1 && (h.Contains("بيع") || h.Contains("سعر") || h.Contains("price") || h.Contains("sell") || h.Contains("retail")))
                    sellIdx = i;
                else if (stockIdx == -1 && (h.Contains("مخزون") || h.Contains("كمية") || h.Contains("رصيد") || h.Contains("stock") || h.Contains("qty") || h.Contains("quantity") || h.Contains("balance")))
                    stockIdx = i;
                else if (minStockIdx == -1 && (h.Contains("أدنى") || h.Contains("ادنى") || h.Contains("طلب") || h.Contains("min") || h.Contains("reorder")))
                    minStockIdx = i;
                else if (categoryIdx == -1 && (h.Contains("قسم") || h.Contains("تصنيف") || h.Contains("مجموعة") || h.Contains("فئة") || h.Contains("category") || h.Contains("dept") || h.Contains("group")))
                    categoryIdx = i;
                else if (unitIdx == -1 && (h.Contains("وحدة") || h.Contains("unit")))
                    unitIdx = i;
                else if (descIdx == -1 && (h.Contains("وصف") || h.Contains("ملاحظ") || h.Contains("desc") || h.Contains("note") || h.Contains("notes")))
                    descIdx = i;
            }
        }

        // Fallback to standard indices if not detected by header text
        if (barcodeIdx == -1) barcodeIdx = 0;
        if (nameIdx == -1) nameIdx = 1;
        if (purchaseIdx == -1) purchaseIdx = 2;
        if (sellIdx == -1) sellIdx = 3;
        if (stockIdx == -1) stockIdx = 4;
        if (minStockIdx == -1) minStockIdx = 5;
        if (categoryIdx == -1) categoryIdx = 6;
        if (unitIdx == -1) unitIdx = 7;
        if (descIdx == -1) descIdx = 8;

        static string GetCol(string[] r, int idx) => (idx >= 0 && idx < r.Length) ? r[idx].Trim() : "";

        static decimal CleanDecimal(string val, decimal def = 0)
        {
            if (string.IsNullOrWhiteSpace(val)) return def;
            var match = System.Text.RegularExpressions.Regex.Match(val, @"(\d[\d,]*(?:\.\d+)?)");
            if (match.Success)
            {
                var clean = match.Value.Replace(",", "");
                if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                    return d;
            }
            return def;
        }

        static int CleanInt(string val, int def = 0)
        {
            if (string.IsNullOrWhiteSpace(val)) return def;
            var match = System.Text.RegularExpressions.Regex.Match(val, @"\d+");
            if (match.Success && int.TryParse(match.Value, out var i))
                return i;
            return def;
        }

        static UnitType ParseUnit(string? u)
        {
            if (string.IsNullOrWhiteSpace(u)) return UnitType.Piece;
            var s = u.Trim().ToLowerInvariant();
            if (s.Contains("box") || s.Contains("علبة") || s.Contains("علبه") || s.Contains("باكت")) return UnitType.Box;
            if (s.Contains("carton") || s.Contains("كرتون") || s.Contains("كرتونة")) return UnitType.Carton;
            if (s.Contains("kg") || s.Contains("كيلو") || s.Contains("كجم")) return UnitType.Kilogram;
            if (s.Contains("liter") || s.Contains("لتر")) return UnitType.Liter;
            return UnitType.Piece;
        }

        int rowNum = 1;
        foreach (var r in dataRows)
        {
            rowNum++;
            try
            {
                var barcode = GetCol(r, barcodeIdx);
                var name = GetCol(r, nameIdx);

                if (string.IsNullOrWhiteSpace(barcode) && string.IsNullOrWhiteSpace(name))
                    continue; // skip completely empty rows

                if (string.IsNullOrWhiteSpace(name))
                {
                    skipped++;
                    errorLog.AppendLine($"- الصف {rowNum}: اسم المنتج فارغ (الباركود: {barcode}).");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(barcode))
                {
                    barcode = $"AUTO-{DateTime.Now:yyMMddHHmmss}-{rowNum}";
                }

                var purchasePrice = CleanDecimal(GetCol(r, purchaseIdx), 0);
                var sellingPrice = CleanDecimal(GetCol(r, sellIdx), purchasePrice > 0 ? purchasePrice * 1.2m : 0);
                var stock = CleanInt(GetCol(r, stockIdx), 0);
                var minStock = CleanInt(GetCol(r, minStockIdx), 5);
                var categoryName = GetCol(r, categoryIdx);
                var unitStr = GetCol(r, unitIdx);
                var unitType = ParseUnit(unitStr);
                var desc = GetCol(r, descIdx);

                // Category Resolution
                int categoryId = existingCategories[0].Id;
                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var matched = existingCategories.FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (matched != null)
                    {
                        categoryId = matched.Id;
                    }
                    else if (autoCreateCategories)
                    {
                        var newCat = new Category { Name = categoryName, Description = "قسم مُنشأ تلقائياً عند الاستيراد", IsActive = true, CreatedAt = DateTime.Now };
                        ctx.Categories.Add(newCat);
                        await ctx.SaveChangesAsync();
                        existingCategories.Add(newCat);
                        categoryId = newCat.Id;
                    }
                }

                // Check for existing product by barcode
                var existingProd = await ctx.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
                if (existingProd != null)
                {
                    if (duplicateAction == "Skip")
                    {
                        skipped++;
                        continue;
                    }
                    else if (duplicateAction == "UpdateStock")
                    {
                        existingProd.Stock += stock;
                        if (sellingPrice > 0) existingProd.SellingPrice = sellingPrice;
                        if (purchasePrice > 0) existingProd.PurchasePrice = purchasePrice;
                        existingProd.UpdatedAt = DateTime.Now;
                        ctx.Products.Update(existingProd);
                        updated++;
                    }
                    else // Overwrite
                    {
                        existingProd.Name = name;
                        existingProd.PurchasePrice = purchasePrice;
                        existingProd.SellingPrice = sellingPrice;
                        existingProd.Stock = stock;
                        existingProd.MinStockLevel = minStock;
                        existingProd.CategoryId = categoryId;
                        if (!string.IsNullOrWhiteSpace(unitStr)) existingProd.Unit = unitType;
                        if (!string.IsNullOrWhiteSpace(desc)) existingProd.Description = desc;
                        existingProd.UpdatedAt = DateTime.Now;
                        ctx.Products.Update(existingProd);
                        updated++;
                    }
                }
                else
                {
                    ctx.Products.Add(new Product
                    {
                        Barcode = barcode,
                        Name = name,
                        PurchasePrice = purchasePrice,
                        SellingPrice = sellingPrice,
                        Stock = stock,
                        MinStockLevel = minStock,
                        Unit = unitType,
                        Description = desc,
                        CategoryId = categoryId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    });
                    added++;
                }
            }
            catch (Exception rowEx)
            {
                skipped++;
                errorLog.AppendLine($"- الصف {rowNum}: خطأ أثناء المعالجة ({rowEx.Message}).");
            }
        }

        await ctx.SaveChangesAsync();

        if (System.Windows.Application.Current?.Dispatcher != null)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadProducts);
        }
        else
        {
            await LoadProducts();
        }

        var summary = $"🎉 اكتمل الاستيراد بنجاح تام!\n\n" +
                      $"➕ منتجات جديدة أُضيفت: {added}\n" +
                      $"🔄 منتجات تم تحديثها: {updated}\n" +
                      $"⏭️ أسطر تم تخطيها: {skipped}";

        return (added, updated, skipped, summary, errorLog.ToString());
    }

    // ════════════════════════════════════════════════════════════════════
    // SAVE
    // ════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        { MessageBox.Show("اسم المنتج مطلوب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        if (string.IsNullOrWhiteSpace(FormBarcode))
        { MessageBox.Show("الباركود مطلوب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        if (FormCategoryId <= 0)
        { MessageBox.Show("يرجى اختيار القسم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var barcodeExists = await ctx.Products.AnyAsync(p =>
                p.Barcode == FormBarcode && !p.IsDeleted && p.Id != (EditingProductId ?? 0));

            if (barcodeExists)
            { MessageBox.Show("الباركود مستخدم لمنتج آخر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

            if (!Enum.TryParse<UnitType>(FormUnit, out var unit)) unit = UnitType.Piece;

            if (EditingProductId.HasValue)
            {
                var entity = await ctx.Products.FindAsync(EditingProductId.Value);
                if (entity == null) return;

                if (entity.Stock != FormStock)
                {
                    ctx.StockMovements.Add(new StockMovement
                    {
                        ProductId = entity.Id,
                        Quantity = FormStock - entity.Stock,
                        Type = MovementType.Adjustment,
                        Reference = "تعديل يدوي (لوحة التحكم)",
                        MovementDate = DateTime.Now
                    });
                }

                entity.Name          = FormName.Trim();
                entity.Barcode       = FormBarcode.Trim();
                entity.PurchasePrice = FormPurchasePrice;
                entity.SellingPrice  = FormSellingPrice;
                entity.Stock         = FormStock;
                entity.MinStockLevel = FormMinStockLevel;
                entity.CategoryId    = FormCategoryId;
                entity.Unit          = unit;
                entity.Description   = FormDescription;
                entity.ImagePath     = FormImagePath;
                entity.IsActive      = FormIsActive;
                entity.UpdatedAt     = DateTime.Now;

                await ctx.SaveChangesAsync();
                MessageBox.Show("تم تحديث المنتج ✅", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var newProduct = new Product
                {
                    Name          = FormName.Trim(),
                    Barcode       = FormBarcode.Trim(),
                    PurchasePrice = FormPurchasePrice,
                    SellingPrice  = FormSellingPrice,
                    Stock         = FormStock,
                    MinStockLevel = FormMinStockLevel,
                    CategoryId    = FormCategoryId,
                    Unit          = unit,
                    Description   = FormDescription,
                    ImagePath     = FormImagePath,
                    IsActive      = FormIsActive,
                    CreatedAt     = DateTime.Now
                };
                
                ctx.Products.Add(newProduct);

                if (FormStock > 0)
                {
                    ctx.StockMovements.Add(new StockMovement
                    {
                        Product = newProduct,
                        Quantity = FormStock,
                        Type = MovementType.Adjustment,
                        Reference = "رصيد افتتاحي (إضافة منتج جديد)",
                        MovementDate = DateTime.Now
                    });
                }

                await ctx.SaveChangesAsync();
                MessageBox.Show("تم إضافة المنتج ✅", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            ClearFormFields();
            await LoadProducts();
        }, EditingProductId.HasValue ? "جاري التحديث..." : "جاري الإضافة...");
    }

    // ════════════════════════════════════════════════════════════════════
    // EDIT  ← يُستدعى من زرّ التعديل في الجدول
    // ════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void EditProduct(Product product)
    {
        EditingProductId  = product.Id;
        FormName          = product.Name;
        FormBarcode       = product.Barcode;
        FormPurchasePrice = product.PurchasePrice;
        FormSellingPrice  = product.SellingPrice;
        FormStock         = product.Stock;
        FormMinStockLevel = product.MinStockLevel;
        FormCategoryId    = product.CategoryId;
        FormUnit          = product.Unit.ToString();
        FormDescription   = product.Description;
        FormImagePath     = product.ImagePath;
        FormIsActive      = product.IsActive;
    }

    // ════════════════════════════════════════════════════════════════════
    // DELETE
    // ════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task DeleteProduct(Product product)
    {
        bool authorized = await _authService.RequestAdminOverrideAsync("حذف منتج من النظام");
        if (!authorized) return;

        if (MessageBox.Show($"حذف المنتج \"{product.Name}\"؟",
            "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var entity = await ctx.Products.FindAsync(product.Id);
            if (entity == null) return;
            entity.IsDeleted  = true;
            entity.UpdatedAt  = DateTime.Now;
            await ctx.SaveChangesAsync();
            await LoadProducts();
        }, "جاري الحذف...");
    }

    // ════════════════════════════════════════════════════════════════════
    // FORM HELPERS
    // ════════════════════════════════════════════════════════════════════

    [RelayCommand]
    private void ClearForm() => ClearFormFields();

    private void ClearFormFields()
    {
        EditingProductId  = null;
        FormName          = string.Empty;
        FormBarcode       = string.Empty;
        FormPurchasePrice = 0;
        FormSellingPrice  = 0;
        FormStock         = 0;
        FormMinStockLevel = 10;
        FormCategoryId    = 0;
        FormUnit          = UnitType.Piece.ToString();
        FormDescription   = null;
        FormImagePath     = null;
        FormIsActive      = true;
    }

    [RelayCommand]
    private void BrowseImage()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "اختر صورة المنتج",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp"
        };
        if (dlg.ShowDialog() == true) FormImagePath = dlg.FileName;
    }
}
