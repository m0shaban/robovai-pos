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

    public ProductsViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser, IAuthorizationService authService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;

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

        var dlg = new SaveFileDialog
        {
            Title = "حفظ قائمة المنتجات في إكسيل",
            FileName = $"المنتجات-{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx",
            Filter = "Excel Files|*.xlsx"
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
                    ws.Cell(1, 8).Value = "وصف";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
                    headerRow.Style.Font.FontColor = XLColor.White;
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int row = 2;
                    foreach (var p in FilteredProducts)
                    {
                        ws.Cell(row, 1).Value = p.Barcode;
                        ws.Cell(row, 2).Value = p.Name;
                        ws.Cell(row, 3).Value = p.PurchasePrice;
                        ws.Cell(row, 4).Value = p.SellingPrice;
                        ws.Cell(row, 5).Value = p.Stock;
                        ws.Cell(row, 6).Value = p.MinStockLevel;
                        ws.Cell(row, 7).Value = p.Unit.ToString();
                        ws.Cell(row, 8).Value = p.Description;
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                });
            }, "جاري التصدير...", $"✅ تم التصدير بنجاح:\n{dlg.FileName}");
        }
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title = "استيراد منتجات من إكسيل",
            Filter = "Excel Files|*.xlsx"
        };

        if (dlg.ShowDialog() != true) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("استيراد قائمة منتجات من ملف إكسيل");
        if (!authorized) return;

        await ExecuteBusyAsync(async () =>
        {
            int added = 0;
            int updated = 0;
            int skipped = 0;
            var errorLog = new System.Text.StringBuilder();

            try
            {
                await Task.Run(async () =>
                {
                    await using var ctx = await _contextFactory.CreateDbContextAsync();
                    using var wb = new XLWorkbook(dlg.FileName);
                    if (wb.Worksheets.Count == 0)
                    {
                        throw new InvalidOperationException("ملف الإكسيل فارغ ولا يحتوي على أي أوراق عمل.");
                    }

                    var ws = wb.Worksheet(1); // Read first sheet
                    var rangeUsed = ws.RangeUsed();
                    if (rangeUsed == null)
                    {
                        throw new InvalidOperationException("ورقة العمل فارغة.");
                    }

                    var rows = rangeUsed.RowsUsed().Skip(1); // Skip header

                    var defaultCat = await ctx.Categories.FirstOrDefaultAsync();
                    if (defaultCat == null)
                    {
                        throw new InvalidOperationException("النظام لا يحتوي على أي أقسام. يرجى إضافة قسم واحد على الأقل قبل استيراد المنتجات.");
                    }

                    foreach (var row in rows)
                    {
                        try
                        {
                            var barcodeCell = row.Cell(1);
                            var nameCell = row.Cell(2);
                            
                            var barcode = barcodeCell?.Value.ToString()?.Trim();
                            var name = nameCell?.Value.ToString()?.Trim();

                            if (string.IsNullOrEmpty(barcode) || string.IsNullOrEmpty(name))
                            {
                                skipped++;
                                errorLog.AppendLine($"- الصف {row.RowNumber()}: تم التخطي لعدم وجود باركود أو اسم.");
                                continue;
                            }

                            decimal.TryParse(row.Cell(3)?.Value.ToString(), out var buyPrice);
                            decimal.TryParse(row.Cell(4)?.Value.ToString(), out var sellPrice);
                            int.TryParse(row.Cell(5)?.Value.ToString(), out var stock);
                            int.TryParse(row.Cell(6)?.Value.ToString(), out var minStock);
                            
                            var unitStr = row.Cell(7)?.Value.ToString()?.Trim();
                            if (!Enum.TryParse<UnitType>(unitStr, out var unitType)) 
                                unitType = UnitType.Piece;
                                
                            var desc = row.Cell(8)?.Value.ToString()?.Trim();

                            var existing = await ctx.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted);

                            if (existing != null)
                            {
                                // Update
                                existing.Name = name;
                                existing.PurchasePrice = buyPrice;
                                existing.SellingPrice = sellPrice;
                                existing.Stock = stock;
                                existing.MinStockLevel = minStock;
                                existing.Unit = unitType;
                                existing.Description = desc;
                                existing.UpdatedAt = DateTime.Now;
                                updated++;
                            }
                            else
                            {
                                // Add
                                ctx.Products.Add(new Product
                                {
                                    Barcode = barcode,
                                    Name = name,
                                    PurchasePrice = buyPrice,
                                    SellingPrice = sellPrice,
                                    Stock = stock,
                                    MinStockLevel = minStock,
                                    Unit = unitType,
                                    Description = desc,
                                    CategoryId = defaultCat.Id,
                                    IsActive = true,
                                    CreatedAt = DateTime.Now
                                });
                                added++;
                            }
                        }
                        catch (Exception rowEx)
                        {
                            skipped++;
                            errorLog.AppendLine($"- الصف {row.RowNumber()}: خطأ في قراءة البيانات ({rowEx.Message}).");
                        }
                    }

                    await ctx.SaveChangesAsync();
                });

                // Reload UI
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(LoadProducts);

                var msg = $"تم الانتهاء من الاستيراد بنجاح.\n\nالمنتجات الجديدة: {added}\nالمنتجات المُحدّثة: {updated}\nالمنتجات المتخطاة: {skipped}";
                if (skipped > 0)
                {
                    msg += "\n\nهل تريد رؤية تفاصيل التخطي والأخطاء؟";
                    if (MessageBox.Show(msg, "نجاح مع وجود ملاحظات", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        MessageBox.Show(errorLog.ToString(), "تفاصيل التخطي", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show(msg, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"لا يمكن قراءة الملف. يرجى التأكد من إغلاق الملف في برنامج Excel والمحاولة مرة أخرى.\nالتفاصيل: {ioEx.Message}", "الملف قيد الاستخدام", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ غير متوقع أثناء الاستيراد:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }, "جاري قراءة واستيراد الملف...");
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
