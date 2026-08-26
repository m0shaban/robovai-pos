# 🚀 Smart POS System - نظام الكاشير الذكي الفضائي

> ملاحظة: هذا الملف يشرح “Space Theme” (Neon/Glass) كتوثيق قديم (Legacy). الواجهة الافتراضية الحالية للبرنامج هي **Al‑Atmani 2026** (Dark + Glass/Acrylic + Bento Dashboard + Touch‑First POS).

> ملاحظة (فبراير 2026): `Themes/SpaceTheme.xaml` لم يعد يتم دمجه تلقائيًا داخل `App.xaml` لتجنب أي تداخل مع Al‑Atmani. إذا احتجته للرجوع للـ Legacy، فعّله من `src/SmartPOS.WPF/appsettings.json` عبر `Ui:EnableLegacySpaceTheme: true`.

## 📋 نظرة عامة

نظام نقاط بيع (POS) احترافي متكامل مع واجهة فضائية عصرية باستخدام **WPF** و **.NET 8.0**. مصمم خصيصاً للشاشات اللمسية مع تأثيرات **Neon** و **Glassmorphism**.

---

## ✨ المميزات الرئيسية المضافة

### 🎨 1. التصميم الفضائي (Space Theme)

- ✅ **Neon Colors**: ألوان نيون متوهجة (#00E5FF Cyan, #B24BF3 Purple, #FF2E97 Pink)
- ✅ **Glassmorphism Effects**: تأثيرات زجاجية شفافة مع blur
- ✅ **Drop Shadow Glow**: توهج نيون حول العناصر
- ✅ **Dark Space Background**: خلفية فضائية داكنة (#0A0E27, #1A1F3A)
- ✅ **Gradient Borders**: حواف متدرجة بألوان نيون
- ✅ **Pulse Animations**: أنيميشنز نبضية للعناصر المهمة

### 📱 2. محسّن للشاشات اللمسية

- ✅ **أزرار كبيرة**: 70-80px ارتفاع
- ✅ **مسافات سخية**: Padding واسع (25px)
- ✅ **بطاقات منتجات عملاقة**: 220x260px
- ✅ **حقول إدخال كبيرة**: 60px ارتفاع
- ✅ **تأثيرات hover واضحة**: تكبير وتوهج

### 🛡️ 3. نظام الورديات (Shift Management)

```csharp
// Shift Model
public class Shift : BaseEntity
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
    public ShiftStatus Status { get; set; }
    public int UserId { get; set; }

    // Computed Properties
    public decimal TotalSales { get; }
    public decimal TotalCash { get; }
    public decimal TotalCard { get; }
    public int TransactionCount { get; }
    public TimeSpan? Duration { get; }
}
```

**المميزات:**

- فتح/إغلاق الورديات
- تسجيل الرصيد الافتتاحي والختامي
- حساب الفرق بين المتوقع والفعلي
- تقارير تفصيلية لكل وردية
- ربط المبيعات بالورديات
- تتبع طرق الدفع (نقدي/بطاقة/تحويل)

### 💎 4. نظام نقاط الولاء (Loyalty Points)

```csharp
// Customer Loyalty Model
public class CustomerLoyalty : BaseEntity
{
    public int Points { get; set; }
    public int TotalPointsEarned { get; set; }
    public int TotalPointsRedeemed { get; set; }
    public LoyaltyTier Tier { get; set; }
    // Bronze → Silver → Gold → Platinum
}
```

**قواعد النقاط:**

- 1 نقطة لكل 10 جنيه مشتريات
- مستويات العضوية:
  - 🥉 Bronze: 0-999 نقطة
  - 🥈 Silver: 1000-2999 نقطة
  - 🥇 Gold: 3000-4999 نقطة
  - 💎 Platinum: 5000+ نقطة

**المميزات:**

- كسب نقاط تلقائي مع كل عملية شراء
- استبدال النقاط بخصومات
- تتبع تاريخ المعاملات
- ترقية تلقائية للمستويات

### 🔄 5. نظام المرتجعات (Returns System)

```csharp
// Return Model
public class Return : BaseEntity
{
    public string ReturnNumber { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public ReturnReason Reason { get; set; }
    public ReturnStatus Status { get; set; }
    public bool IsRefunded { get; set; }
    public int SaleId { get; set; }
    public int CustomerId { get; set; }
}
```

**المميزات:**

- إرجاع كامل أو جزئي
- أسباب متعددة (معيب/خطأ/طلب العميل)
- استرجاع المبلغ أو رصيد محل
- إعادة المنتجات للمخزون تلقائياً
- حالات متعددة (معلق/موافق/مرفوض/مكتمل)

### 📄 6. طباعة فواتير مع QR Code

```csharp
// في Sale Model
public string? QRCode { get; set; }
public bool IsPrinted { get; set; }
```

**المميزات:**

- QR Code يحتوي على:
  - رقم الفاتورة
  - التاريخ والوقت
  - المبلغ الإجمالي
  - رابط للتحقق من الفاتورة
- طباعة حرارية 80mm/58mm
- تصميم احترافي بشعار المحل
- معلومات العميل ونقاط الولاء
- تفاصيل طرق الدفع

### ⌨️ 7. اختصارات لوحة المفاتيح

| المفتاح    | الوظيفة               |
| ---------- | --------------------- |
| **F1**     | 💡 المساعدة والمميزات |
| **F2**     | 🔍 التركيز على البحث  |
| **F3**     | 👤 اختيار عميل        |
| **F4**     | ⚙️ الإعدادات          |
| **F5**     | 🔄 تحديث المنتجات     |
| **F9**     | 🛡️ إدارة الوردية      |
| **F10**    | 📊 التقارير           |
| **F12**    | 💰 الدفع السريع       |
| **Ctrl+R** | 🔄 وضع الإرجاع        |
| **Ctrl+D** | 💳 خصم                |
| **Ctrl+H** | ⏸️ احتفاظ             |
| **Ctrl+N** | 🆕 فاتورة جديدة       |
| **Esc**    | ❌ إلغاء/إفراغ السلة  |
| **Enter**  | ✅ تأكيد/إضافة        |

### 💾 8. نسخ احتياطي تلقائي

```csharp
// Backup Service
public class BackupService
{
    public async Task CreateDailyBackup();
    public async Task RestoreBackup(string backupPath);
    public List<BackupInfo> GetAvailableBackups();
}
```

**المميزات:**

- نسخ تلقائي يومي الساعة 2 صباحاً
- حفظ في `Backups/YYYY-MM-DD/pos_backup.db`
- الاحتفاظ بآخر 30 يوم
- استعادة بنقرة واحدة
- ضغط الملفات (ZIP)

---

## 🎨 الأنماط والتصميم

### Glass Card (Glassmorphism)

```xaml
<Border Style="{StaticResource GlassCard}">
    <!-- المحتوى -->
</Border>
```

### Neon Button

```xaml
<Button Style="{StaticResource NeonButton}"
        Content="دفع"
        Click="Pay_Click"/>
```

### Touch Button (للشاشات اللمسية)

```xaml
<Button Style="{StaticResource TouchButton}"
        Content="منتج 1"/>
```

### Product Touch Card

```xaml
<Border Style="{StaticResource ProductTouchCard}">
    <StackPanel>
        <!-- صورة المنتج -->
        <!-- اسم المنتج -->
        <!-- السعر -->
    </StackPanel>
</Border>
```

### Neon Text Styles

```xaml
<!-- عنوان رئيسي -->
<TextBlock Style="{StaticResource NeonHeader}"
           Text="نقطة البيع الذكية"/>

<!-- عنوان فرعي -->
<TextBlock Style="{StaticResource NeonSubHeader}"
           Text="الوردية النشطة"/>

<!-- نص عادي -->
<TextBlock Style="{StaticResource NeonText}"
           Text="المجموع: 500 ج.م"/>
```

---

## 🏗️ هيكل المشروع المحدث

```
SmartPOS/
├── SmartPOS.Core/
│   └── Entities/
│       ├── Product.cs
│       ├── Category.cs
│       ├── Customer.cs (محدث - مع Loyalty)
│       ├── Sale.cs (محدث - مع QR & Shift)
│       ├── SaleDetail.cs
│       ├── User.cs
│       ├── Supplier.cs
│       ├── Expense.cs
│       ├── ⭐ Shift.cs (جديد)
│       ├── ⭐ CustomerLoyalty.cs (جديد)
│       ├── ⭐ LoyaltyTransaction.cs (جديد)
│       ├── ⭐ Return.cs (جديد)
│       └── ⭐ ReturnDetail.cs (جديد)
│
├── SmartPOS.WPF/
│   ├── ⭐ Themes/
│   │   └── SpaceTheme.xaml (جديد - التصميم الفضائي)
│   ├── Views/
│   │   ├── MainWindow.xaml (محدث - خلفية فضائية)
│   │   ├── POSPage.xaml (محدث - Touch Optimized)
│   │   ├── DashboardPage.xaml
│   │   ├── ProductsPage.xaml
│   │   ├── ReportsPage.xaml
│   │   ├── ⭐ ShiftManagementPage.xaml (جديد)
│   │   ├── ⭐ ReturnsPage.xaml (جديد)
│   │   └── ⭐ LoyaltyManagementPage.xaml (جديد)
│   └── Services/
│       ├── ⭐ ShiftService.cs (جديد)
│       ├── ⭐ LoyaltyService.cs (جديد)
│       ├── ⭐ QRCodeService.cs (جديد)
│       ├── ⭐ PrintService.cs (محدث - مع QR)
│       └── ⭐ BackupService.cs (جديد)
│
└── SmartPOS.Infrastructure/
    └── Data/
        └── AppDbContext.cs (محدث - مع Entities الجديدة)
```

---

## 📦 المكتبات المطلوبة

إضافة إلى الـ NuGet Packages الموجودة:

```xml
<ItemGroup>
    <!-- QR Code Generation -->
    <PackageReference Include="QRCoder" Version="1.4.3" />

    <!-- Advanced Reporting & Charts -->
    <PackageReference Include="LiveCharts.Wpf" Version="0.9.7" />
    <PackageReference Include="ClosedXML" Version="0.102.1" />

    <!-- Thermal Printing -->
    <PackageReference Include="ESCPOS_NET" Version="4.0.0" />

    <!-- PDF Generation -->
    <PackageReference Include="iTextSharp.LGPLv2.Core" Version="3.4.2" />
</ItemGroup>
```

---

## 🎯 خطوات التطبيق

### 1. إضافة الـ Entities الجديدة

✅ تم إنشاء:

- `Shift.cs`
- `CustomerLoyalty.cs`
- `LoyaltyTransaction.cs`
- `Return.cs`
- `ReturnDetail.cs`

### 2. تحديث DbContext

```csharp
public class AppDbContext : DbContext
{
    // Existing DbSets...

    // New DbSets
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<CustomerLoyalty> CustomerLoyalties { get; set; }
    public DbSet<LoyaltyTransaction> LoyaltyTransactions { get; set; }
    public DbSet<Return> Returns { get; set; }
    public DbSet<ReturnDetail> ReturnDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // تكوين العلاقات الجديدة
        modelBuilder.Entity<CustomerLoyalty>()
            .HasOne(cl => cl.Customer)
            .WithOne(c => c.Loyalty)
            .HasForeignKey<CustomerLoyalty>(cl => cl.CustomerId);

        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Shift)
            .WithMany(sh => sh.Sales)
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 3. إضافة Migration

```bash
cd F:\Raw\kasher\src\SmartPOS.Infrastructure
dotnet ef migrations add AddShiftsLoyaltyReturns
dotnet ef database update
```

### 4. إنشاء Services

#### ShiftService.cs

```csharp
public class ShiftService : IShiftService
{
    public async Task<Shift> OpenShift(int userId, decimal openingBalance);
    public async Task<Shift> CloseShift(int shiftId, decimal closingBalance);
    public async Task<Shift?> GetActiveShift(int userId);
    public async Task<ShiftReport> GetShiftReport(int shiftId);
}
```

#### LoyaltyService.cs

```csharp
public class LoyaltyService : ILoyaltyService
{
    public int CalculatePoints(decimal amount);
    public async Task AddPoints(int customerId, int points, int saleId);
    public async Task RedeemPoints(int customerId, int points);
    public LoyaltyTier CalculateTier(int totalPoints);
}
```

#### QRCodeService.cs

```csharp
public class QRCodeService : IQRCodeService
{
    public byte[] GenerateQRCode(Sale sale);
    public string GenerateQRData(Sale sale);
}
```

### 5. تحديث POSPage للشاشات اللمسية

استبدال البطاقات الصغيرة ببطاقات Touch:

```xaml
<!-- بدلاً من -->
<materialDesign:Card Width="150" Height="180">

<!-- استخدم -->
<Border Style="{StaticResource ProductTouchCard}">
    <Button Style="{StaticResource TouchButton}"
            Height="240"
            Click="AddProduct_Click"
            Tag="{Binding}">
        <StackPanel>
            <!-- صورة 120x120 -->
            <Image Source="{Binding ImagePath}"
                   Width="120" Height="120"/>

            <!-- اسم المنتج -->
            <TextBlock Text="{Binding Name}"
                       Style="{StaticResource NeonText}"
                       FontSize="16"
                       FontWeight="Bold"
                       TextAlignment="Center"
                       Margin="0,10,0,5"/>

            <!-- السعر -->
            <TextBlock FontSize="20"
                       Foreground="#00E5FF"
                       FontWeight="Bold"
                       TextAlignment="Center">
                <Run Text="{Binding SellingPrice, StringFormat='{}{0:F2}'}"/>
                <Run Text=" ج.م"/>
            </TextBlock>
        </StackPanel>
    </Button>
</Border>
```

### 6. إنشاء صفحة إدارة الورديات

```xaml
<!-- ShiftManagementPage.xaml -->
<Page Background="{StaticResource SpaceGradient}">
    <Grid Margin="30">
        <!-- Header -->
        <Border Style="{StaticResource GlassCard}">
            <TextBlock Style="{StaticResource NeonHeader}"
                       Text="🛡️ إدارة الورديات"/>
        </Border>

        <!-- Active Shift Card -->
        <Border Style="{StaticResource NeonStatCard}">
            <StackPanel>
                <TextBlock Style="{StaticResource NeonSubHeader}"
                           Text="الوردية النشطة"/>
                <TextBlock Style="{StaticResource NeonText}"
                           FontSize="48">
                    <Run Text="{Binding CurrentShift.TotalSales, StringFormat='{}{0:C}'}"/>
                </TextBlock>
            </StackPanel>
        </Border>

        <!-- Open/Close Buttons -->
        <Button Style="{StaticResource NeonButton}"
                Content="🟢 فتح وردية"
                Command="{Binding OpenShiftCommand}"/>
        <Button Style="{StaticResource NeonButton}"
                Content="🔴 إغلاق وردية"
                Command="{Binding CloseShiftCommand}"/>
    </Grid>
</Page>
```

---

## 🚀 الميزات المستقبلية

- [ ] تطبيق موبايل للمراقبة عن بعد
- [ ] مزامنة سحابية بين الفروع
- [ ] تكامل مع بوابات الدفع الإلكتروني
- [ ] إدارة الطاولات للمطاعم
- [ ] تقارير BI متقدمة مع Power BI
- [ ] دعم الفواتير الإلكترونية
- [ ] تكامل مع WhatsApp Business API

---

## 📝 الملاحظات الهامة

1. **الأداء**: استخدم `VirtualizingStackPanel` للقوائم الطويلة
2. **الأمان**: تشفير كلمات المرور باستخدام BCrypt
3. **النسخ الاحتياطي**: تفعيل النسخ التلقائي اليومي
4. **الطباعة**: اختبار الطابعة قبل الإنتاج
5. **الشاشات اللمسية**: استخدام `TouchButton` دائماً

---

## 🎨 لقطات الشاشة المتوقعة

### 1. الشاشة الرئيسية (Space Theme)

- خلفية فضائية متدرجة داكنة
- بطاقات منتجات بتوهج نيون أزرق/بنفسجي
- أزرار كبيرة مع تأثيرات Glassmorphism
- نصوص متوهجة بألوان النيون

### 2. إدارة الورديات

- بطاقة الوردية النشطة مع توهج
- إحصائيات لحظية (مبيعات/نقدي/بطاقة)
- مؤقت للوردية
- رسوم بيانية نيون

### 3. نقاط الولاء

- عرض نقاط العميل مع مستوى العضوية
- شارات (Badges) ملونة للمستويات
- تاريخ المعاملات
- أنيميشن عند كسب نقاط

---

## 📞 الدعم والتوثيق

للحصول على الكود الكامل والملفات المفقودة:

1. تواصل معي لإرسال الملفات الكاملة
2. الوثائق التفصيلية لكل Service
3. أمثلة تطبيقية كاملة

---

**تم التطوير بواسطة GitHub Copilot** 🤖
**الإصدار**: 2.0.0 - Space Edition 🌌
**التاريخ**: فبراير 2026
