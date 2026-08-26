# 🎯 دليل البدء السريع - Smart POS (Space Edition - Legacy)

> ملاحظة 2026: تم تحديث واجهة البرنامج لتكون **Al‑Atmani 2026** (وضع داكن + Glass/Acrylic + Bento Dashboard + Touch‑First POS). محتوى هذا الملف يركّز على إضافات Space Edition ويُعتبر مرجعاً تاريخياً.

## 📝 ملخص التحديثات

### ✅ ما تم إضافته:

#### 1. النماذج الجديدة (Entities)

- ✅ `Shift.cs` - نظام الورديات الكامل
- ✅ `CustomerLoyalty.cs` - نقاط الولاء
- ✅ `LoyaltyTransaction.cs` - معاملات النقاط
- ✅ `Return.cs` - المرتجعات
- ✅ `ReturnDetail.cs` - تفاصيل المرتجعات

#### 2. التحديثات على النماذج الموجودة

- ✅ `Customer.cs` - إضافة علاقة Loyalty و Returns
- ✅ `Sale.cs` - إضافة QR Code, Shift, LoyaltyPoints

#### 3. التصميم الفضائي

- ✅ `SpaceTheme.xaml` - ملف كامل للتصميم الفضائي مع:
  - ألوان Neon (#00E5FF, #B24BF3, #FF2E97)
  - تأثيرات Glassmorphism
  - أنماط للشاشات اللمسية
  - توهج Drop Shadow
  - أنيميشنز Pulse

- ✅ `App.xaml` - الواجهة الافتراضية الحالية **Al‑Atmani 2026**. يمكن تفعيل SpaceTheme (Legacy) اختياريًا من `src/SmartPOS.WPF/appsettings.json` عبر `Ui:EnableLegacySpaceTheme: true`.

---

## 🔧 الخطوات التالية للتطبيق الكامل

### الخطوة 1: تحديث قاعدة البيانات

```bash
# الانتقال لمجلد Infrastructure
cd F:\Raw\kasher\src\SmartPOS.Infrastructure

# إنشاء Migration جديد
dotnet ef migrations add AddSpacePOSFeatures --project ../SmartPOS.Infrastructure.csproj --startup-project ../SmartPOS.WPF/SmartPOS.WPF.csproj

# تطبيق التحديثات على قاعدة البيانات
dotnet ef database update --project ../SmartPOS.Infrastructure.csproj --startup-project ../SmartPOS.WPF/SmartPOS.WPF.csproj
```

### الخطوة 2: تحديث DbContext

افتح `SmartPOS.Infrastructure/Data/AppDbContext.cs` وأضف:

```csharp
public DbSet<Shift> Shifts { get; set; }
public DbSet<CustomerLoyalty> CustomerLoyalties { get; set; }
public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
public DbSet<Return> Returns { get; set; }
public DbSet<ReturnDetail> ReturnDetails { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Loyalty - Customer One-to-One
    modelBuilder.Entity<CustomerLoyalty>()
        .HasOne(cl => cl.Customer)
        .WithOne(c => c.Loyalty)
        .HasForeignKey<CustomerLoyalty>(cl => cl.CustomerId)
        .OnDelete(DeleteBehavior.Cascade);

    // Sale - Shift Relationship
    modelBuilder.Entity<Sale>()
        .HasOne(s => s.Shift)
        .WithMany(sh => sh.Sales)
        .HasForeignKey(s => s.ShiftId)
        .OnDelete(DeleteBehavior.Restrict);

    // Return - Sale Relationship
    modelBuilder.Entity<Return>()
        .HasOne(r => r.Sale)
        .WithMany(s => s.Returns)
        .HasForeignKey(r => r.SaleId)
        .OnDelete(DeleteBehavior.Restrict);
}
```

### الخطوة 3: إضافة الـ NuGet Packages

```bash
cd F:\Raw\kasher\src\SmartPOS.Infrastructure

# QR Code
dotnet add package QRCoder

# Charts للتقارير
cd ../SmartPOS.WPF
dotnet add package LiveCharts.Wpf

# Excel Export
dotnet add package ClosedXML

# PDF Generation
dotnet add package iTextSharp.LGPLv2.Core

# Thermal Printing (Optional)
dotnet add package ESCPOS_NET
```

### الخطوة 4: إنشاء الـ Services

#### ShiftService.cs

في `SmartPOS.Application/Services/`:

```csharp
public interface IShiftService
{
    Task<Shift> OpenShiftAsync(int userId, decimal openingBalance);
    Task<Shift> CloseShiftAsync(int shiftId, decimal closingBalance, string? notes);
    Task<Shift?> GetActiveShiftAsync(int userId);
    Task<bool> HasActiveShiftAsync(int userId);
    Task<List<Shift>> GetShiftHistoryAsync(DateTime? from, DateTime? to);
}

public class ShiftService : IShiftService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Shift> OpenShiftAsync(int userId, decimal openingBalance)
    {
        // Check if user has active shift
        var activeShift = await _unitOfWork.Shifts
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open);

        if (activeShift != null)
            throw new InvalidOperationException("يوجد وردية نشطة بالفعل");

        var shift = new Shift
        {
            UserId = userId,
            StartTime = DateTime.Now,
            OpeningBalance = openingBalance,
            Status = ShiftStatus.Open
        };

        await _unitOfWork.Shifts.AddAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        return shift;
    }

    public async Task<Shift> CloseShiftAsync(int shiftId, decimal closingBalance, string? notes)
    {
        var shift = await _unitOfWork.Shifts
            .Include(s => s.Sales)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null)
            throw new NotFoundException("الوردية غير موجودة");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException("الوردية مغلقة بالفعل");

        shift.EndTime = DateTime.Now;
        shift.ClosingBalance = closingBalance;
        shift.ExpectedBalance = shift.OpeningBalance + shift.TotalCash;
        shift.Difference = closingBalance - shift.ExpectedBalance.Value;
        shift.Status = ShiftStatus.Closed;
        shift.Notes = notes;

        await _unitOfWork.SaveChangesAsync();

        return shift;
    }
}
```

#### LoyaltyService.cs

```csharp
public interface ILoyaltyService
{
    Task<int> CalculateAndAddPointsAsync(int customerId, decimal saleAmount, int saleId);
    Task<bool> RedeemPointsAsync(int customerId, int points);
    Task<CustomerLoyalty?> GetCustomerLoyaltyAsync(int customerId);
    LoyaltyTier CalculateTier(int totalPoints);
}

public class LoyaltyService : ILoyaltyService
{
    private readonly IUnitOfWork _unitOfWork;
    private const decimal POINTS_PER_AMOUNT = 10m; // 1 point per 10 EGP

    public async Task<int> CalculateAndAddPointsAsync(int customerId, decimal saleAmount, int saleId)
    {
        var points = (int)(saleAmount / POINTS_PER_AMOUNT);

        if (points <= 0) return 0;

        var loyalty = await _unitOfWork.CustomerLoyalties
            .FirstOrDefaultAsync(cl => cl.CustomerId == customerId);

        if (loyalty == null)
        {
            loyalty = new CustomerLoyalty
            {
                CustomerId = customerId,
                Points = points,
                TotalPointsEarned = points,
                Tier = CalculateTier(points)
            };
            await _unitOfWork.CustomerLoyalties.AddAsync(loyalty);
        }
        else
        {
            loyalty.Points += points;
            loyalty.TotalPointsEarned += points;
            var newTier = CalculateTier(loyalty.TotalPointsEarned);
            if (newTier != loyalty.Tier)
            {
                loyalty.Tier = newTier;
                loyalty.LastTierUpdate = DateTime.Now;
            }
        }

        // Add transaction
        var transaction = new LoyaltyTransaction
        {
            CustomerLoyaltyId = loyalty.Id,
            SaleId = saleId,
            Points = points,
            Type = LoyaltyTransactionType.Earned,
            RelatedAmount = saleAmount,
            Description = $"كسب نقاط من عملية شراء بمبلغ {saleAmount:C}"
        };

        await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return points;
    }

    public LoyaltyTier CalculateTier(int totalPoints)
    {
        if (totalPoints >= 5000) return LoyaltyTier.Platinum;
        if (totalPoints >= 3000) return LoyaltyTier.Gold;
        if (totalPoints >= 1000) return LoyaltyTier.Silver;
        return LoyaltyTier.Bronze;
    }
}
```

#### QRCodeService.cs

```csharp
using QRCoder;

public interface IQRCodeService
{
    byte[] GenerateQRCode(Sale sale);
    string GenerateQRData(Sale sale);
}

public class QRCodeService : IQRCodeService
{
    public byte[] GenerateQRCode(Sale sale)
    {
        var qrData = GenerateQRData(sale);

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(20);
    }

    public string GenerateQRData(Sale sale)
    {
        // Format: InvoiceNumber|Date|Total|Customer
        return $"{sale.InvoiceNumber}|{sale.SaleDate:yyyy-MM-dd HH:mm}|{sale.TotalAmount:F2}|{sale.Customer?.Name ?? "عميل عام"}";
    }
}
```

### الخطوة 5: تحديث POSPage للشاشات اللمسية

في `POSPage.xaml`، استبدل Grid المنتجات:

```xaml
<!-- بدلاً من ScrollViewer عادي -->
<ScrollViewer Grid.Column="1"
              VerticalScrollBarVisibility="Auto">
    <ItemsControl ItemsSource="{Binding Products}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <WrapPanel Orientation="Horizontal"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <!-- بطاقة المنتج Touch Optimized -->
                <Border Style="{StaticResource ProductTouchCard}"
                        Margin="10">
                    <Button Style="{StaticResource TouchButton}"
                            Command="{Binding DataContext.AddProductCommand,
                                    RelativeSource={RelativeSource AncestorType=Page}}"
                            CommandParameter="{Binding}"
                            Background="Transparent"
                            BorderThickness="0">
                        <StackPanel>
                            <!-- صورة المنتج -->
                            <Border Width="140"
                                    Height="140"
                                    Background="{StaticResource SpaceDeepBrush}"
                                    CornerRadius="12"
                                    Margin="0,0,0,15">
                                <materialDesign:PackIcon Kind="Package"
                                                         Width="80"
                                                         Height="80"
                                                         Foreground="{StaticResource NeonCyanBrush}"/>
                            </Border>

                            <!-- اسم المنتج -->
                            <TextBlock Text="{Binding Name}"
                                       Style="{StaticResource NeonText}"
                                       FontSize="16"
                                       FontWeight="Bold"
                                       TextAlignment="Center"
                                       TextTrimming="CharacterEllipsis"
                                       MaxWidth="180"
                                       Margin="0,0,0,8"/>

                            <!-- السعر -->
                            <TextBlock TextAlignment="Center"
                                       FontSize="22"
                                       FontWeight="Bold"
                                       Foreground="{StaticResource NeonGreenBrush}">
                                <Run Text="{Binding SellingPrice, StringFormat='{}{0:F2}'}"/>
                                <Run Text=" ج.م"/>
                            </TextBlock>

                            <!-- المخزون -->
                            <TextBlock Text="{Binding Stock, StringFormat='متوفر: {0}'}"
                                       Style="{StaticResource NeonText}"
                                       FontSize="12"
                                       TextAlignment="Center"
                                       Opacity="0.7"
                                       Margin="0,5,0,0"/>
                        </StackPanel>
                    </Button>
                </Border>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</ScrollViewer>
```

### الخطوة 6: إضافة صفحة إدارة الورديات

إنشاء `Views/ShiftManagementPage.xaml`:

```xaml
<Page x:Class="SmartPOS.WPF.Views.ShiftManagementPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
      Background="{StaticResource SpaceGradient}"
      FontFamily="Segoe UI, Tahoma"
      FlowDirection="RightToLeft">

    <Grid Margin="30">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0"
                Style="{StaticResource GlassCard}"
                Margin="0,0,0,30">
            <StackPanel Orientation="Horizontal">
                <materialDesign:PackIcon Kind="Shield"
                                         Width="48"
                                         Height="48"
                                         Foreground="{StaticResource NeonCyanBrush}"
                                         Margin="0,0,20,0"/>
                <TextBlock Style="{StaticResource NeonHeader}"
                           Text="🛡️ إدارة الورديات"
                           VerticalAlignment="Center"
                           Margin="0"/>
            </StackPanel>
        </Border>

        <!-- Active Shift Info -->
        <Grid Grid.Row="1" Margin="0,0,0,30">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <!-- Total Sales -->
            <Border Grid.Column="0"
                    Style="{StaticResource NeonStatCard}"
                    BorderBrush="{StaticResource NeonGreenBrush}"
                    Margin="0,0,15,0">
                <StackPanel>
                    <TextBlock Style="{StaticResource NeonText}"
                               Text="إجمالي المبيعات"
                               FontSize="16"
                               Opacity="0.8"
                               Margin="0,0,0,10"/>
                    <TextBlock FontSize="36"
                               FontWeight="Bold"
                               Foreground="{StaticResource NeonGreenBrush}">
                        <Run Text="{Binding ActiveShift.TotalSales, StringFormat='{}{0:F2}', FallbackValue='0.00'}"/>
                        <Run Text=" ج.م" FontSize="24"/>
                    </TextBlock>
                    <TextBlock Style="{StaticResource NeonText}"
                               FontSize="14"
                               Opacity="0.6"
                               Margin="0,5,0,0">
                        <Run Text="{Binding ActiveShift.TransactionCount, FallbackValue='0'}"/>
                        <Run Text=" عملية"/>
                    </TextBlock>
                </StackPanel>
            </Border>

            <!-- Cash -->
            <Border Grid.Column="1"
                    Style="{StaticResource NeonStatCard}"
                    BorderBrush="{StaticResource NeonCyanBrush}"
                    Margin="7.5,0">
                <StackPanel>
                    <TextBlock Style="{StaticResource NeonText}"
                               Text="نقدي"
                               FontSize="16"
                               Opacity="0.8"
                               Margin="0,0,0,10"/>
                    <TextBlock FontSize="36"
                               FontWeight="Bold"
                               Foreground="{StaticResource NeonCyanBrush}">
                        <Run Text="{Binding ActiveShift.TotalCash, StringFormat='{}{0:F2}', FallbackValue='0.00'}"/>
                        <Run Text=" ج.م" FontSize="24"/>
                    </TextBlock>
                </StackPanel>
            </Border>

            <!-- Card -->
            <Border Grid.Column="2"
                    Style="{StaticResource NeonStatCard}"
                    BorderBrush="{StaticResource NeonPurpleBrush}"
                    Margin="15,0,0,0">
                <StackPanel>
                    <TextBlock Style="{StaticResource NeonText}"
                               Text="بطاقة"
                               FontSize="16"
                               Opacity="0.8"
                               Margin="0,0,0,10"/>
                    <TextBlock FontSize="36"
                               FontWeight="Bold"
                               Foreground="{StaticResource NeonPurpleBrush}">
                        <Run Text="{Binding ActiveShift.TotalCard, StringFormat='{}{0:F2}', FallbackValue='0.00'}"/>
                        <Run Text=" ج.م" FontSize="24"/>
                    </TextBlock>
                </StackPanel>
            </Border>
        </Grid>

        <!-- Action Buttons -->
        <StackPanel Grid.Row="2"
                    Orientation="Horizontal"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Top">
            <Button Style="{StaticResource NeonButton}"
                    Width="250"
                    Margin="0,0,20,0"
                    Command="{Binding OpenShiftCommand}">
                <StackPanel Orientation="Horizontal">
                    <materialDesign:PackIcon Kind="PlayCircle"
                                             Width="32"
                                             Height="32"
                                             Margin="0,0,15,0"/>
                    <TextBlock Text="🟢 فتح وردية جديدة"
                               FontSize="18"
                               VerticalAlignment="Center"/>
                </StackPanel>
            </Button>

            <Button Style="{StaticResource NeonButton}"
                    Width="250"
                    Command="{Binding CloseShiftCommand}">
                <StackPanel Orientation="Horizontal">
                    <materialDesign:PackIcon Kind="StopCircle"
                                             Width="32"
                                             Height="32"
                                             Margin="0,0,15,0"/>
                    <TextBlock Text="🔴 إغلاق الوردية"
                               FontSize="18"
                               VerticalAlignment="Center"/>
                </StackPanel>
            </Button>
        </StackPanel>
    </Grid>
</Page>
```

---

## 🎯 اختبار النظام

```bash
# بناء المشروع
cd F:\Raw\kasher\src\SmartPOS.WPF
dotnet build

# تشغيل
dotnet run
```

---

## ✅ Checklist التطبيق

- [x] إضافة Entities الجديدة
- [x] تحديث Customer و Sale
- [x] إنشاء Space Theme
- [x] تحديث App.xaml للثيم الداكن
- [ ] تحديث DbContext
- [ ] إضافة Migration
- [ ] إنشاء Services (Shift, Loyalty, QR)
- [ ] تحديث POSPage للشاشات اللمسية
- [ ] إنشاء ShiftManagementPage
- [ ] إنشاء ReturnsPage
- [ ] إضافة Keyboard Shortcuts
- [ ] إضافة BackupService
- [ ] تحديث Printing Service مع QR
- [ ] إضافة LiveCharts للتقارير

---

**🚀 ابدأ الآن وحول نظامك إلى تحفة فضائية!**
