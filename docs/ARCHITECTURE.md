# Smart POS - Architecture Documentation

## 📐 Clean Architecture Overview

This project follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────────────────────────────────┐
│           Presentation Layer (WPF)              │
│  ┌─────────────────────────────────────────┐   │
│  │         Views (XAML)                     │   │
│  │  • MainWindow, DashboardPage, POSPage   │   │
│  └─────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────┘
                   │ Depends on ↓
┌─────────────────────────────────────────────────┐
│          Application Layer                      │
│  ┌─────────────────────────────────────────┐   │
│  │         ViewModels (Business Logic)      │   │
│  │  • MainPOSViewModel, DashboardViewModel │   │
│  │         DTOs (Data Transfer Objects)     │   │
│  │  • CartItem                             │   │
│  └─────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────┘
                   │ Depends on ↓
┌─────────────────────────────────────────────────┐
│         Infrastructure Layer                    │
│  ┌─────────────────────────────────────────┐   │
│  │    Data Access (EF Core)                │   │
│  │  • AppDbContext, Repositories           │   │
│  │    External Services                    │   │
│  │  • PrintingService, BarcodeService      │   │
│  └─────────────────────────────────────────┘   │
└──────────────────┬──────────────────────────────┘
                   │ Depends on ↓
┌─────────────────────────────────────────────────┐
│              Core Layer (Domain)                │
│  ┌─────────────────────────────────────────┐   │
│  │         Entities                        │   │
│  │  • Product, Sale, User, Expense        │   │
│  │         Interfaces                      │   │
│  │  • IRepository, IPrintingService       │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

## 🗂️ Project Structure

### SmartPOS.Core (Domain Layer)

**Purpose**: Core business entities and interfaces. No dependencies on other projects.

**Key Components**:

- **Entities**: Business objects (Product, Sale, User, etc.)
- **Interfaces**: Contracts for services (IRepository, IPrintingService, etc.)
- **Enums**: Business logic enumerations (PaymentMethod, UserRole, etc.)

**Dependencies**: None (pure C#)

---

### SmartPOS.Infrastructure (Data & Services Layer)

**Purpose**: Data access and external service implementations.

**Key Components**:

- **Data/AppDbContext.cs**: Entity Framework Core database context
- **Repositories**: Generic repository pattern implementation
- **Services/PrintingService.cs**: ESC/POS thermal printer service
- **Services/BarcodeService.cs**: Barcode scanner integration

**Dependencies**: SmartPOS.Core

**Technologies**:

- Entity Framework Core 8.0
- SQLite / SQL Server
- System.Drawing (for printing)

---

### SmartPOS.Application (Business Logic Layer)

**Purpose**: Application-specific business logic and ViewModels.

**Key Components**:

- **ViewModels/MainPOSViewModel.cs**: POS cashier logic
  - Cart management
  - Payment processing
  - Barcode scanning
  - Receipt printing
- **ViewModels/DashboardViewModel.cs**: Analytics and reporting
  - Sales statistics
  - Low stock alerts
  - Recent transactions

- **DTOs/CartItem.cs**: Data transfer objects for UI binding

**Dependencies**: SmartPOS.Core, SmartPOS.Infrastructure

**Technologies**:

- CommunityToolkit.Mvvm (MVVM framework)
- Microsoft.Extensions.DependencyInjection

---

### SmartPOS.WPF (Presentation Layer)

**Purpose**: User interface and presentation logic.

**Key Components**:

- **Views/MainWindow.xaml**: Main application shell with navigation
- **Views/DashboardPage.xaml**: Analytics dashboard
- **Views/POSPage.xaml**: POS cashier interface
- **App.xaml**: Application resources and dependency injection setup

### UI Theme (Al‑Atmani 2026)

The current presentation layer uses a dark-mode-first design system with glass/acrylic surfaces:

- Acrylic sidebar surface
- Glass cards for dashboard + panels
- Bento-style dashboard layout
- Touch-first cashier layout

Legacy “SpaceTheme / Space Edition” docs may still exist in the repository as historical reference.

Theme note (Feb 2026): `Themes/SpaceTheme.xaml` is disabled by default to prevent resource collisions/visual bleed. Enable explicitly via `src/SmartPOS.WPF/appsettings.json` → `Ui:EnableLegacySpaceTheme: true`.

---

## 🗄️ SQLite Notes (Aggregations)

SQLite can be strict when aggregating `decimal` values (for example, `SUM(decimal)` projections). For totals in reporting/shift/dashboard paths, the code aggregates as `double?` and then converts back to `decimal`.

## 🖨️ Printing Notes (Virtual Printers)

When printing receipts/reports, the app resolves the printer name using this preference order:

1. Saved printer (from settings)
2. OS default printer
3. First available non-virtual printer (avoids OneNote/PDF/XPS/Fax)

**Dependencies**: SmartPOS.Application, SmartPOS.Infrastructure, SmartPOS.Core

**Technologies**:

- WPF (.NET 8)
- MaterialDesignInXamlToolkit
- Microsoft.Extensions.Hosting (for DI)

---

## 🗄️ Database Schema

### Entity Relationships

```
┌─────────────┐       ┌──────────────┐
│   Category  │◄──────│   Product    │
└─────────────┘   1:N └──────┬───────┘
                             │ 1:N
                             ▼
                      ┌──────────────┐
                      │  SaleDetail  │
                      └──────┬───────┘
                             │ N:1
                             ▼
┌─────────────┐       ┌──────────────┐
│     User    │◄──────│     Sale     │
└─────────────┘   1:N └──────────────┘

┌─────────────┐       ┌──────────────┐
│  Supplier   │◄──────│   Product    │
└─────────────┘   1:N └──────────────┘
```

### Key Tables

**Product**

- Core inventory entity
- Tracks stock levels, pricing, and categories
- Supports multiple units (Piece, Box, Carton, etc.)
- Low stock alerts via `MinStockLevel`

**Sale & SaleDetail**

- Header-detail pattern for transactions
- Supports discounts, taxes, and multiple payment methods
- Tracks profit per transaction

**User**

- Role-based access (Admin, Manager, Cashier, Inventory)
- Tracks who performed transactions

**Expense**

- Categorized expense tracking
- Integrated with profit/loss calculations

---

## 🖨️ Thermal Printer Integration

### ESC/POS Commands

The `PrintingService` uses raw ESC/POS commands for thermal printers:

**Supported Features**:

- ✅ Text formatting (bold, underline, size)
- ✅ Alignment (left, center, right)
- ✅ Receipt printing with logo
- ✅ Barcode printing
- ✅ Cash drawer trigger
- ✅ Paper cutting
- ✅ Z-Report generation

**Usage**:

```csharp
var receiptData = new ReceiptData
{
    StoreName = "My Store",
    InvoiceNumber = "INV-001",
    Items = cartItems,
    TotalAmount = 100.00m
};

await _printingService.PrintReceiptAsync("PrinterName", receiptData);
```

---

## 🎯 POS Features

### Keyboard Shortcuts (Touch-Optimized)

- **F1**: Focus barcode input
- **F2**: Increase quantity
- **F3**: Decrease quantity
- **F4**: Remove item
- **F5**: Apply discount
- **F7**: Hold sale
- **F8**: Open cash drawer
- **F9**: Complete sale
- **F10**: Clear cart

### Barcode Scanner Integration

- HID mode support (plug-and-play)
- Automatic product lookup
- Buffer-based input detection
- Configurable timeout

### Payment Processing

- Multiple payment methods (Cash, Card, Mobile)
- Split payment support
- Automatic change calculation
- Real-time total updates

---

## 📊 Dashboard Analytics

**Real-time Metrics**:

- Today's sales and profit
- Transaction count
- Monthly sales
- Low stock alerts

**Visual Components**:

- Statistics cards with icons
- Recent transactions table
- Low stock product list
- Sales charts (extensible)

---

## 🔧 Configuration

### Database Provider

Edit `appsettings.json`:

**SQLite (Default)**:

```json
"DatabaseProvider": "SQLite",
"ConnectionStrings": {
  "DefaultConnection": "Data Source=smartpos.db"
}
```

**SQL Server**:

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=SmartPOS;Trusted_Connection=True;"
}
```

### Store Settings

Configure in `appsettings.json`:

- Store name and address
- Phone and email
- Tax rate
- Receipt footer
- Low stock threshold

---

## 🚀 Getting Started

### 1. Restore Dependencies

```bash
cd F:\Raw\kasher
dotnet restore
```

### 2. Build Solution

```bash
dotnet build
```

### 3. Create Database

```bash
cd src/SmartPOS.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../SmartPOS.WPF
dotnet ef database update --startup-project ../SmartPOS.WPF
```

### 4. Run Application

```bash
cd src/SmartPOS.WPF
dotnet run
```

**Default Login**:

- Username: `admin`
- Password: `admin123`

---

## 🧪 Testing

### Test Barcode Scanner

1. Open POS Cashier
2. Click barcode input (or press F1)
3. Scan barcode or type manually
4. Press Enter

### Test Thermal Printer

1. Go to Settings > Printer Settings
2. Select your thermal printer
3. Click "Test Print"
4. Cash drawer should open if connected

---

## 📦 Deployment

### Publish for Windows

```bash
dotnet publish src/SmartPOS.WPF -c Release -r win-x64 --self-contained
```

**Output**: `src/SmartPOS.WPF/bin/Release/net8.0-windows/win-x64/publish/`

### Create Installer

Use tools like:

- WiX Toolset
- Inno Setup
- Advanced Installer

---

## 🔐 Security Considerations

**Current Implementation**:

- Password hashing (BCrypt recommended)
- Soft delete for data integrity
- Transaction logging via stock movements

**Production Recommendations**:

- Implement proper authentication (JWT, OAuth)
- Add user activity logging
- Enable backup scheduling
- Implement role-based permissions

---

## 🎨 Customization

### Change Theme Colors

Edit `App.xaml`:

```xml
<materialDesign:BundledTheme
    BaseTheme="Light"
    PrimaryColor="DeepPurple"  <!-- Change here -->
    SecondaryColor="Lime" />    <!-- Change here -->
```

**Available Colors**:
Red, Pink, Purple, DeepPurple, Indigo, Blue, LightBlue, Cyan, Teal, Green, LightGreen, Lime, Yellow, Amber, Orange, DeepOrange, Brown, Grey, BlueGrey

### Add Custom Reports

1. Create new ViewModel in Application layer
2. Create corresponding View in WPF layer
3. Add navigation item in MainWindow.xaml
4. Implement report logic using LINQ queries

---

## 🐛 Troubleshooting

### Database Issues

```bash
# Reset database
cd src/SmartPOS.Infrastructure
dotnet ef database drop --startup-project ../SmartPOS.WPF
dotnet ef database update --startup-project ../SmartPOS.WPF
```

### Printer Not Working

1. Verify printer is in "RAW" mode
2. Check printer name matches exactly
3. Test with Notepad print
4. Verify USB/Network connection

### Barcode Scanner Not Detected

1. Ensure HID mode is enabled
2. Test scanner in Notepad
3. Check barcode format compatibility
4. Adjust timeout in BarcodeService

---

## 📚 Additional Resources

- [Material Design Icons](https://materialdesignicons.com/)
- [ESC/POS Command Reference](https://reference.epson-biz.com/modules/ref_escpos/)
- [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)
- [WPF Documentation](https://docs.microsoft.com/dotnet/desktop/wpf/)

---

**Version**: 1.0.0  
**Last Updated**: February 2026  
**License**: Proprietary
