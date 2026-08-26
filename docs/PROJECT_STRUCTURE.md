# 📁 Complete Project Structure

> Theme note (Feb 2026): The app defaults to **Al‑Atmani 2026**. Legacy `src/SmartPOS.WPF/Themes/SpaceTheme.xaml` is kept for historical reference and is disabled by default. Toggle via `src/SmartPOS.WPF/appsettings.json` → `Ui:EnableLegacySpaceTheme`.

## Directory Tree

```
F:\Raw\kasher\
│
├── SmartPOS.sln                           # Solution file
├── README.md                              # Project overview
├── QUICKSTART.md                          # User guide & tutorial
├── ARCHITECTURE.md                        # Technical architecture documentation
├── IMPLEMENTATION_SUMMARY.md              # Complete implementation details
├── BUILD_DEPLOY.md                        # Build & deployment guide
├── DIAGRAMS.md                            # Visual system diagrams
├── .gitignore                             # Git ignore configuration
│
└── src/
    │
    ├── SmartPOS.Core/                     # 🎯 DOMAIN LAYER (450 lines)
    │   ├── SmartPOS.Core.csproj           # Project file
    │   │
    │   ├── Entities/                      # Business entities (12 files)
    │   │   ├── BaseEntity.cs              # Base class with common properties
    │   │   ├── Product.cs                 # Product entity with stock tracking
    │   │   ├── Category.cs                # Product categories
    │   │   ├── Sale.cs                    # Transaction header
    │   │   ├── SaleDetail.cs              # Transaction line items
    │   │   ├── User.cs                    # System users with roles
    │   │   ├── Expense.cs                 # Business expenses
    │   │   ├── Supplier.cs                # Vendor management
    │   │   ├── Customer.cs                # Customer records
    │   │   ├── PurchaseOrder.cs           # Purchase orders
    │   │   └── StockMovement.cs           # Inventory audit trail
    │   │
    │   └── Interfaces/                    # Service contracts (5 files)
    │       ├── IRepository.cs             # Generic repository pattern
    │       ├── IUnitOfWork.cs             # Transaction management
    │       ├── IPrintingService.cs        # Thermal printer interface + DTOs
    │       ├── IBarcodeService.cs         # Barcode scanner interface
    │       └── IReportService.cs          # Report generation interface
    │
    ├── SmartPOS.Infrastructure/           # 💾 DATA ACCESS LAYER (850 lines)
    │   ├── SmartPOS.Infrastructure.csproj # Project file with EF Core packages
    │   │
    │   ├── Data/                          # Database context
    │   │   └── AppDbContext.cs            # EF Core DbContext (200 lines)
    │   │                                  # • All DbSets
    │   │                                  # • Fluent API configuration
    │   │                                  # • Relationships & indexes
    │   │                                  # • Seed data
    │   │
    │   ├── Repositories/                  # Data access implementation
    │   │   ├── Repository.cs              # Generic repository implementation
    │   │   └── UnitOfWork.cs              # Transaction management
    │   │
    │   └── Services/                      # External services
    │       ├── PrintingService.cs         # ESC/POS thermal printer (450 lines)
    │       │                              # • PrintReceiptAsync()
    │       │                              # • PrintZReportAsync()
    │       │                              # • OpenCashDrawer()
    │       │                              # • Raw ESC/POS commands
    │       │                              # • Windows printer API integration
    │       │
    │       └── BarcodeService.cs          # HID barcode scanner
    │                                      # • Event-driven architecture
    │                                      # • Buffer-based detection
    │
    ├── SmartPOS.Application/              # 🧠 BUSINESS LOGIC LAYER (520 lines)
    │   ├── SmartPOS.Application.csproj    # Project file with MVVM toolkit
    │   │
    │   ├── ViewModels/                    # MVVM ViewModels
    │   │   ├── MainPOSViewModel.cs        # POS cashier logic (345 lines)
    │   │   │                              # • Cart management
    │   │   │                              # • Barcode scanning
    │   │   │                              # • Payment processing
    │   │   │                              # • Receipt printing
    │   │   │                              # • Keyboard shortcuts (F1-F10)
    │   │   │                              # • Real-time calculations
    │   │   │
    │   │   └── DashboardViewModel.cs      # Dashboard analytics (120 lines)
    │   │                                  # • Today's sales & profit
    │   │                                  # • Transaction count
    │   │                                  # • Low stock alerts
    │   │                                  # • Recent sales list
    │   │
    │   └── DTOs/                          # Data Transfer Objects
    │       └── CartItem.cs                # Cart item with computed properties
    │
    └── SmartPOS.WPF/                      # 🎨 PRESENTATION LAYER (650 lines)
        ├── SmartPOS.WPF.csproj            # WPF project file
        │                                  # • Material Design packages
        │                                  # • Microsoft.Extensions.Hosting
        │
      ├── App.xaml                       # Application resources
      │                                  # • Material Design theme (Dark)
      │                                  # • Al-Atmani 2026 palette (Deep Space + Electric Cyan)
      │                                  # • Acrylic/Glass styles + shared controls
        │
        ├── App.xaml.cs                    # Dependency Injection setup (50 lines)
        │                                  # • Service registration
        │                                  # • DbContext configuration
        │                                  # • Database initialization
        │
        ├── appsettings.json               # Configuration file
        │                                  # • Connection strings
        │                                  # • Store settings
        │                                  # • Printer configuration
        │
        └── Views/                         # WPF Views
            ├── MainWindow.xaml            # Main shell (135 lines)
            │                              # • Drawer navigation
            │                              # • Top app bar
            │                              # • Frame-based routing
            │                              # • Menu items (6)
            │
            ├── MainWindow.xaml.cs         # Code-behind (40 lines)
            │                              # • Navigation logic
            │                              # • Logout handler
            │
            ├── DashboardPage.xaml         # Dashboard UI
            │                              # • Bento grid layout (dark/glass cards)
            │                              # • Stats cards + recent sales (compact items)
            │                              # • Low stock list
            │
            ├── DashboardPage.xaml.cs      # Dashboard logic (60 lines)
            │                              # • ViewModel binding
            │                              # • Data refresh
            │
            ├── POSPage.xaml               # POS Cashier UI
            │                              # • Touch-first dark/glass layout
            │                              # • Barcode input + product touch cards
            │                              # • Cart panel + payment panel
            │                              # • Large checkout CTA + keyboard shortcuts
            │
            └── POSPage.xaml.cs            # POS logic (90 lines)
                                           # • Keyboard shortcut handling
                                           # • ViewModel binding
```

---

## File Count Summary

| Category           | Count        | Lines of Code    |
| ------------------ | ------------ | ---------------- |
| **Entity Classes** | 12           | ~300             |
| **Interfaces**     | 5            | ~150             |
| **Data Access**    | 3            | ~400             |
| **Services**       | 2            | ~450             |
| **ViewModels**     | 2            | ~465             |
| **DTOs**           | 1            | ~55              |
| **Views (XAML)**   | 3            | ~635             |
| **Code-Behind**    | 4            | ~240             |
| **Configuration**  | 3            | ~100             |
| **Documentation**  | 7            | N/A              |
| **Total**          | **42 files** | **~2,795 lines** |

---

## Layer Breakdown

### 🎯 Core Layer (Domain)

**Purpose**: Business entities and contracts  
**Dependencies**: None (pure C#)  
**Files**: 17  
**Lines**: ~450

```
SmartPOS.Core/
├── Entities/          12 entity classes
└── Interfaces/        5 service contracts
```

**Key Features**:

- ✅ Business entities with computed properties
- ✅ Enum definitions for business logic
- ✅ Service interface definitions
- ✅ No external dependencies

---

### 💾 Infrastructure Layer

**Purpose**: Data access and external services  
**Dependencies**: SmartPOS.Core, EF Core, System.Drawing  
**Files**: 5  
**Lines**: ~850

```
SmartPOS.Infrastructure/
├── Data/              EF Core DbContext
├── Repositories/      Generic repository + UoW
└── Services/          Printing, Barcode services
```

**Key Features**:

- ✅ Entity Framework Core 8.0
- ✅ SQLite & SQL Server support
- ✅ ESC/POS thermal printing (450 lines)
- ✅ HID barcode scanner integration
- ✅ Repository pattern implementation

---

### 🧠 Application Layer

**Purpose**: Business logic and ViewModels  
**Dependencies**: Core, Infrastructure, MVVM Toolkit  
**Files**: 3  
**Lines**: ~520

```
SmartPOS.Application/
├── ViewModels/        2 ViewModels
└── DTOs/             1 DTO class
```

**Key Features**:

- ✅ MainPOSViewModel (345 lines) - Complete POS logic
- ✅ DashboardViewModel (120 lines) - Analytics
- ✅ ObservableObject pattern
- ✅ RelayCommand for UI actions
- ✅ Async/await operations

---

### 🎨 Presentation Layer (WPF)

**Purpose**: User interface  
**Dependencies**: All layers, Material Design  
**Files**: 7  
**Lines**: ~650

```
SmartPOS.WPF/
├── Views/            3 XAML pages + code-behind
├── App.xaml         Theme & resources
└── App.xaml.cs      DI setup
```

**Key Features**:

- ✅ Material Design UI (Modern aesthetics)
- ✅ Responsive layout (Touch & keyboard)
- ✅ MVVM data binding
- ✅ Navigation system
- ✅ Keyboard shortcuts (F1-F10)

---

## Documentation Files

| File                          | Purpose                         | Lines            |
| ----------------------------- | ------------------------------- | ---------------- |
| **README.md**                 | Project overview                | ~100             |
| **QUICKSTART.md**             | User guide & setup              | ~300             |
| **ARCHITECTURE.md**           | Technical documentation         | ~500             |
| **IMPLEMENTATION_SUMMARY.md** | Complete implementation details | ~400             |
| **BUILD_DEPLOY.md**           | Build & deployment guide        | ~450             |
| **DIAGRAMS.md**               | Visual system diagrams          | ~350             |
| **.gitignore**                | Git configuration               | ~50              |
| **Total Documentation**       |                                 | **~2,150 lines** |

---

## Technology Stack by Layer

### Core Layer

- C# 12
- .NET 8.0

### Infrastructure Layer

- Entity Framework Core 8.0
- SQLite Provider
- SQL Server Provider
- System.Drawing (Printing)

### Application Layer

- CommunityToolkit.Mvvm 8.2.2
- Microsoft.Extensions.DependencyInjection

### Presentation Layer

- WPF (.NET 8)
- MaterialDesignThemes 5.0.0
- MaterialDesignColors 3.0.0
- Microsoft.Extensions.Hosting 8.0.0

---

## Configuration Files

### SmartPOS.Core.csproj

```xml
<TargetFramework>net8.0</TargetFramework>
<Nullable>enable</Nullable>
```

### SmartPOS.Infrastructure.csproj

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
```

### SmartPOS.Application.csproj

```xml
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

### SmartPOS.WPF.csproj

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net8.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<PackageReference Include="MaterialDesignThemes" Version="5.0.0" />
```

---

## Key Code Files

### Largest Files (by line count)

1. **PrintingService.cs** - 450 lines
   - ESC/POS command implementation
   - Receipt formatting
   - Z-Report generation
   - Hardware integration

2. **MainPOSViewModel.cs** - 345 lines
   - Cart management logic
   - Payment processing
   - Real-time calculations
   - Keyboard shortcuts

3. **POSPage.xaml** - 280 lines
   - POS user interface
   - DataGrid with cart items
   - Payment panel
   - Quick action buttons

4. **DashboardPage.xaml** - 220 lines
   - Statistics cards
   - Recent sales grid
   - Low stock alerts
   - Visual analytics

5. **AppDbContext.cs** - 200 lines
   - Entity configuration
   - Relationships
   - Indexes
   - Seed data

---

## Project Metrics

### Code Statistics

- **Total Files**: 42
- **Total Code Lines**: ~2,795
- **Documentation Lines**: ~2,150
- **Total Project Lines**: ~4,945

### Complexity Distribution

- **Simple** (< 100 lines): 25 files (60%)
- **Medium** (100-300 lines): 12 files (29%)
- **Complex** (> 300 lines): 5 files (11%)

### Test Coverage

- Unit Tests: Ready for implementation
- Integration Tests: Ready for implementation
- UI Tests: Ready for implementation

---

## Extensibility Points

### Easy to Add

```
src/SmartPOS.WPF/Views/
├── ProductsPage.xaml          # Product management (TODO)
├── ReportsPage.xaml           # Advanced reports (TODO)
├── ExpensesPage.xaml          # Expense tracking (TODO)
├── SettingsPage.xaml          # System settings (TODO)
├── UsersPage.xaml             # User management (TODO)
└── SuppliersPage.xaml         # Supplier management (TODO)
```

### New ViewModels

```
src/SmartPOS.Application/ViewModels/
├── ProductsViewModel.cs       # Product CRUD (TODO)
├── ReportsViewModel.cs        # Report generation (TODO)
├── ExpensesViewModel.cs       # Expense tracking (TODO)
└── SettingsViewModel.cs       # Settings management (TODO)
```

---

## Database Files (Runtime)

```
src/SmartPOS.WPF/
├── smartpos.db                # SQLite database (created at runtime)
├── smartpos.db-shm            # SQLite shared memory
└── smartpos.db-wal            # SQLite write-ahead log
```

---

## Build Output

### Debug Build

```
src/SmartPOS.WPF/bin/Debug/net8.0-windows/
├── SmartPOS.WPF.exe
├── SmartPOS.Core.dll
├── SmartPOS.Infrastructure.dll
├── SmartPOS.Application.dll
├── EntityFramework*.dll
├── MaterialDesign*.dll
└── (other dependencies)
```

### Release Build (Published)

```
src/SmartPOS.WPF/bin/Release/net8.0-windows/win-x64/publish/
├── SmartPOS.WPF.exe           # Main executable
├── smartpos.db                # Database file
├── appsettings.json           # Configuration
└── (all dependencies bundled)
```

---

**Project Structure Documentation Complete!**

This provides a complete visual map of the entire codebase, making it easy to:

- Navigate the project
- Understand file organization
- Locate specific functionality
- Plan extensions
- Onboard new developers

Version: 1.0.0  
Last Updated: February 2026
