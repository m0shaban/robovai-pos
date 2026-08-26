# 🎨 System Architecture Diagrams

> Note: The Al‑Atmani 2026 UI redesign (dark/glass/bento) is a presentation-layer update and does not change the core Clean Architecture layout shown below.

> Theme note (Feb 2026): Legacy `Themes/SpaceTheme.xaml` exists for historical reference and is disabled by default. Enable via `src/SmartPOS.WPF/appsettings.json` → `Ui:EnableLegacySpaceTheme: true`.

## 📊 High-Level System Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                        SMART POS SYSTEM                              │
│                     Point of Sale & Mini-ERP                         │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
         ┌──────────▼────────┐ ┌───▼────┐ ┌───────▼───────┐
         │   POS CASHIER     │ │DASHBOARD│ │   INVENTORY   │
         │  Fast Checkout    │ │Analytics│ │  Management   │
         └──────────┬────────┘ └────┬────┘ └───────┬───────┘
                    │               │               │
         ┌──────────▼───────────────▼───────────────▼──────┐
         │         APPLICATION BUSINESS LOGIC               │
         │    ViewModels • Services • DTOs                  │
         └──────────────────────┬──────────────────────────┘
                                │
         ┌──────────────────────▼──────────────────────────┐
         │          INFRASTRUCTURE LAYER                    │
         │  Database • Printing • Barcode • Reporting      │
         └──────────────────────┬──────────────────────────┘
                                │
         ┌──────────────────────▼──────────────────────────┐
         │              CORE DOMAIN                         │
         │    Entities • Interfaces • Business Rules       │
         └──────────────────────────────────────────────────┘
```

---

## 🏗️ Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER (UI)                          │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  WPF Application (SmartPOS.WPF)                              │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐            │  │
│  │  │ MainWindow │  │ Dashboard  │  │  POS Page  │            │  │
│  │  │   .xaml    │  │   Page     │  │   .xaml    │            │  │
│  │  └────────────┘  └────────────┘  └────────────┘            │  │
│  │         Material Design UI • MVVM Pattern                    │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ Data Binding
┌─────────────────────────────▼───────────────────────────────────────┐
│              APPLICATION LAYER (Business Logic)                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  SmartPOS.Application                                        │  │
│  │  ┌──────────────────┐  ┌──────────────────┐                 │  │
│  │  │  MainPOSViewModel│  │DashboardViewModel│                 │  │
│  │  │  • Cart Logic    │  │  • Analytics     │                 │  │
│  │  │  • Checkout      │  │  • Reports       │                 │  │
│  │  │  • Calculations  │  │  • Statistics    │                 │  │
│  │  └──────────────────┘  └──────────────────┘                 │  │
│  │  ┌──────────────────────────────────────────────────┐       │  │
│  │  │            DTOs (Data Transfer Objects)          │       │  │
│  │  │            CartItem, ReportData, etc.            │       │  │
│  │  └──────────────────────────────────────────────────┘       │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ Uses Services
┌─────────────────────────────▼───────────────────────────────────────┐
│           INFRASTRUCTURE LAYER (Data Access & Services)             │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  SmartPOS.Infrastructure                                     │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │  │
│  │  │  AppDbContext│  │ Repositories │  │  Services    │      │  │
│  │  │  EF Core     │  │ Generic Repo │  │  • Printing  │      │  │
│  │  │  • Products  │  │ Unit of Work │  │  • Barcode   │      │  │
│  │  │  • Sales     │  │              │  │  • Reports   │      │  │
│  │  │  • Users     │  │              │  │              │      │  │
│  │  └──────────────┘  └──────────────┘  └──────────────┘      │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────┬───────────────────────────────────────┘
                              │ Implements
┌─────────────────────────────▼───────────────────────────────────────┐
│                 CORE LAYER (Domain)                                 │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │  SmartPOS.Core (No Dependencies)                             │  │
│  │  ┌────────────────────┐  ┌────────────────────┐             │  │
│  │  │     Entities       │  │    Interfaces      │             │  │
│  │  │  • Product         │  │  • IRepository<T>  │             │  │
│  │  │  • Sale            │  │  • IUnitOfWork     │             │  │
│  │  │  • SaleDetail      │  │  • IPrintingService│             │  │
│  │  │  • User            │  │  • IBarcodeService │             │  │
│  │  │  • Expense         │  │  • IReportService  │             │  │
│  │  │  • Supplier        │  │                    │             │  │
│  │  │  • Category        │  │                    │             │  │
│  │  └────────────────────┘  └────────────────────┘             │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🗄️ Database Entity Relationship Diagram

```
┌─────────────────┐
│    Category     │
│─────────────────│
│ •Id             │
│  Name           │
│  Description    │
│  ColorCode      │
│  IsActive       │
└────────┬────────┘
         │ 1
         │
         │ N
┌────────▼────────┐           ┌─────────────────┐
│    Product      │           │    Supplier     │
│─────────────────│           │─────────────────│
│ •Id             │◄──────────│ •Id             │
│  Barcode        │    N:1    │  Name           │
│  Name           │           │  ContactPerson  │
│  PurchasePrice  │           │  Phone          │
│  SellingPrice   │           │  DebtAmount     │
│  Stock          │           └─────────────────┘
│  MinStockLevel  │
│  Unit           │
│ •CategoryId     │
│ •SupplierId     │
└────────┬────────┘
         │ 1
         │
         │ N
┌────────▼────────┐     N     ┌─────────────────┐
│  SaleDetail     │◄──────────│      Sale       │
│─────────────────│           │─────────────────│
│ •Id             │      1    │ •Id             │
│  Quantity       │           │  InvoiceNumber  │
│  UnitPrice      │           │  SaleDate       │
│  UnitCost       │           │  Subtotal       │
│  DiscountAmount │           │  DiscountAmount │
│  LineTotal      │           │  TotalAmount    │
│ •SaleId         │           │  PaymentMethod  │
│ •ProductId      │           │  Status         │
└─────────────────┘           │ •UserId         │
                              │ •CustomerId     │
                              └────────┬────────┘
                                       │
                                       │ N:1
                              ┌────────▼────────┐
                              │      User       │
                              │─────────────────│
                              │ •Id             │
                              │  Username       │
                              │  PasswordHash   │
                              │  FullName       │
                              │  Role           │
                              │  IsActive       │
                              └─────────────────┘

┌─────────────────┐     1     ┌─────────────────┐
│  StockMovement  │◄──────────│    Product      │
│─────────────────│      N    │─────────────────│
│ •Id             │           │ (shown above)   │
│ •ProductId      │           └─────────────────┘
│  Quantity       │
│  Type           │
│  Reference      │
│  MovementDate   │
└─────────────────┘

┌─────────────────┐     N     ┌─────────────────┐
│ PurchaseOrder   │───────────│    Supplier     │
│─────────────────│      1    │─────────────────│
│ •Id             │           │ (shown above)   │
│  OrderNumber    │           └─────────────────┘
│  OrderDate      │
│  TotalAmount    │
│  PaidAmount     │
│  Status         │
│ •SupplierId     │
└─────────────────┘

┌─────────────────┐     N     ┌─────────────────┐
│    Expense      │───────────│      User       │
│─────────────────│      1    │─────────────────│
│ •Id             │           │ (shown above)   │
│  Description    │           └─────────────────┘
│  Amount         │
│  ExpenseDate    │
│  Category       │
│ •UserId         │
└─────────────────┘

Legend:
• Primary/Foreign Key
─ Relationship Line
```

---

## 🔄 POS Transaction Flow

```
┌──────────────────────────────────────────────────────────────────┐
│                      POS TRANSACTION FLOW                        │
└──────────────────────────────────────────────────────────────────┘

    User Action              System Process              Result
        │                         │                         │
┌───────▼────────┐         ┌──────▼──────┐         ┌──────▼──────┐
│  Scan Barcode  │────────▶│   Lookup    │────────▶│   Product   │
│   or Type      │         │  Product    │         │   Found     │
└────────────────┘         └─────────────┘         └──────┬──────┘
                                                           │
                                                    ┌──────▼──────┐
                                                    │   Validate  │
                                                    │    Stock    │
                                                    └──────┬──────┘
                                                           │
                                              ┌────────────▼────────────┐
                                              │  Add/Update Cart Item   │
                                              └────────────┬────────────┘
                                                           │
                                              ┌────────────▼────────────┐
                                              │   Calculate Totals      │
                                              │  • Subtotal             │
                                              │  • Discount             │
                                              │  • Tax                  │
                                              │  • Total                │
                                              └────────────┬────────────┘
                                                           │
┌────────────────┐                            ┌────────────▼────────────┐
│ Enter Payment  │───────────────────────────▶│   Validate Payment      │
│    Amount      │                            │   Amount >= Total       │
└────────────────┘                            └────────────┬────────────┘
                                                           │
                                              ┌────────────▼────────────┐
                                              │  Calculate Change       │
                                              └────────────┬────────────┘
                                                           │
┌────────────────┐                            ┌────────────▼────────────┐
│   Press F9     │───────────────────────────▶│   Complete Sale         │
│  Complete Sale │                            │  • Create Sale Record   │
└────────────────┘                            │  • Create SaleDetails   │
                                              │  • Update Product Stock │
                                              │  • Log Stock Movement   │
                                              └────────────┬────────────┘
                                                           │
                                              ┌────────────▼────────────┐
                                              │    Print Receipt        │
                                              │  • Format ESC/POS       │
                                              │  • Send to Printer      │
                                              │  • Open Drawer          │
                                              └────────────┬────────────┘
                                                           │
                                              ┌────────────▼────────────┐
                                              │    Clear Cart           │
                                              │  Ready for Next Sale    │
                                              └─────────────────────────┘
```

---

## 🖨️ Thermal Printer Communication

```
┌──────────────────────────────────────────────────────────────────┐
│              THERMAL PRINTER ESC/POS FLOW                        │
└──────────────────────────────────────────────────────────────────┘

SmartPOS App                PrintingService              Thermal Printer
     │                            │                             │
     │  PrintReceiptAsync()       │                             │
     ├───────────────────────────▶│                             │
     │                            │                             │
     │                            │  Build ESC/POS Commands     │
     │                            │  ┌────────────────────┐    │
     │                            │  │ INIT               │    │
     │                            │  │ ALIGN_CENTER       │    │
     │                            │  │ BOLD_ON            │    │
     │                            │  │ "STORE NAME"       │    │
     │                            │  │ BOLD_OFF           │    │
     │                            │  │ ...items...        │    │
     │                            │  │ "Total: $100.00"   │    │
     │                            │  │ CUT_PAPER          │    │
     │                            │  └────────────────────┘    │
     │                            │                             │
     │                            │  OpenPrinter()              │
     │                            ├────────────────────────────▶│
     │                            │◄────────────────────────────│
     │                            │  Handle: 0x1234             │
     │                            │                             │
     │                            │  WritePrinter(rawBytes)     │
     │                            ├────────────────────────────▶│
     │                            │                             │
     │                            │                    ┌────────▼────────┐
     │                            │                    │  Interpret      │
     │                            │                    │  ESC/POS        │
     │                            │                    │  Commands       │
     │                            │                    └────────┬────────┘
     │                            │                             │
     │                            │                    ┌────────▼────────┐
     │                            │                    │   Print to      │
     │                            │                    │  Thermal Paper  │
     │                            │                    └────────┬────────┘
     │                            │                             │
     │                            │                    ┌────────▼────────┐
     │                            │                    │   Cut Paper     │
     │                            │                    └─────────────────┘
     │                            │                             │
     │                            │  Success                    │
     │                            │◄────────────────────────────│
     │                            │                             │
     │                            │  ClosePrinter()             │
     │                            ├────────────────────────────▶│
     │                            │                             │
     │  Receipt Printed ✓         │                             │
     │◄───────────────────────────│                             │
     │                            │                             │
```

---

## 📊 Data Flow Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                      DATA FLOW OVERVIEW                          │
└──────────────────────────────────────────────────────────────────┘

     USER INPUT                 SYSTEM                  OUTPUT
        │                         │                        │
        │                         │                        │
┌───────▼───────┐          ┌──────▼──────┐         ┌─────▼─────┐
│  Barcode      │─────────▶│  Product    │────────▶│   Cart    │
│  Scanner      │          │  Lookup     │         │  Display  │
└───────────────┘          └─────────────┘         └───────────┘
        │                         │                        │
┌───────▼───────┐          ┌──────▼──────┐         ┌─────▼─────┐
│  Keyboard     │─────────▶│  ViewModel  │────────▶│   UI      │
│  Shortcuts    │          │  Logic      │         │  Update   │
└───────────────┘          └─────────────┘         └───────────┘
        │                         │                        │
┌───────▼───────┐          ┌──────▼──────┐         ┌─────▼─────┐
│  Touch        │─────────▶│  Validation │────────▶│  Error    │
│  Input        │          │  Rules      │         │  Message  │
└───────────────┘          └─────────────┘         └───────────┘
        │                         │                        │
┌───────▼───────┐          ┌──────▼──────┐         ┌─────▼─────┐
│  Complete     │─────────▶│  Database   │────────▶│  Receipt  │
│  Sale (F9)    │          │  Save       │         │  Print    │
└───────────────┘          └──────┬──────┘         └───────────┘
                                  │
                          ┌───────▼────────┐
                          │  Stock Update  │
                          │  Audit Trail   │
                          └────────────────┘
```

---

## 🔐 Security Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    SECURITY LAYERS                               │
└──────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  USER INTERFACE LAYER                                           │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • Login Screen                                         │   │
│  │  • Session Management                                   │   │
│  │  • Role-Based UI Elements                               │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│  APPLICATION SECURITY                                           │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • User Authentication                                  │   │
│  │  • Role-Based Authorization                             │   │
│  │  • Business Rule Validation                             │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│  DATA SECURITY                                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • Password Hashing (BCrypt)                            │   │
│  │  • SQL Injection Prevention (Parameterized Queries)     │   │
│  │  • Soft Delete (Data Integrity)                         │   │
│  │  • Audit Trail (Stock Movements)                        │   │
│  └─────────────────────────────────────────────────────────┘   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│  INFRASTRUCTURE SECURITY                                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  • Database Connection Encryption                       │   │
│  │  • File System Permissions                              │   │
│  │  • Backup Encryption                                    │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 MVVM Pattern Implementation

```
┌──────────────────────────────────────────────────────────────────┐
│                    MVVM PATTERN IN SMART POS                     │
└──────────────────────────────────────────────────────────────────┘

        VIEW                  VIEWMODEL                   MODEL
        (UI)              (Business Logic)            (Data/Entities)
         │                      │                           │
┌────────▼─────────┐   ┌────────▼─────────┐      ┌─────────▼────────┐
│  POSPage.xaml    │   │ MainPOSViewModel │      │   Product        │
│  ┌─────────────┐ │   │ ┌──────────────┐ │      │   Sale           │
│  │  TextBox    │◄├───┤│  BarcodeInput│◄├──────┤   SaleDetail     │
│  │  (Barcode)  │ │   │└──────────────┘ │      │   User           │
│  └─────────────┘ │   │                 │      └──────────────────┘
│                  │   │ ┌──────────────┐ │              ▲
│  ┌─────────────┐ │   │ │  CartItems   │ │              │
│  │  DataGrid   │◄├───┤│(Observable   │ │              │
│  │  (Cart)     │ │   │ │ Collection)  │ │       ┌──────┴──────┐
│  └─────────────┘ │   │ └──────────────┘ │       │  DbContext  │
│                  │   │                 │ │       │  Repository │
│  ┌─────────────┐ │   │ ┌──────────────┐ │       └─────────────┘
│  │  Button     │─┼──▶│ │ RelayCommand │ │              ▲
│  │  (Complete) │ │   │ │ (Complete    │ │              │
│  └─────────────┘ │   │ │  Sale)       │─┼──────────────┘
│                  │   │ └──────────────┘ │
│  ┌─────────────┐ │   │                 │
│  │  TextBlock  │◄├───┤│  TotalAmount │ │
│  │  (Total)    │ │   │ └──────────────┘ │
│  └─────────────┘ │   └──────────────────┘
└──────────────────┘
        ▲                      ▲
        │                      │
        │   Data Binding       │
        └──────────────────────┘
        │                      │
        │   Commands           │
        └──────────────────────┘
        │                      │
        │   Events             │
        └──────────────────────┘
```

---

## 📱 System Components Interaction

```
┌──────────────────────────────────────────────────────────────────┐
│              COMPONENT INTERACTION DIAGRAM                       │
└──────────────────────────────────────────────────────────────────┘

┌───────────────┐        ┌───────────────┐        ┌───────────────┐
│   WPF Views   │        │  ViewModels   │        │  Services     │
│               │        │               │        │               │
│ • MainWindow  │───────▶│ • MainPOSVM   │───────▶│ • Printing    │
│ • Dashboard   │        │ • DashboardVM │        │ • Barcode     │
│ • POSPage     │        │               │        │ • Reports     │
└───────┬───────┘        └───────┬───────┘        └───────┬───────┘
        │                        │                        │
        │ Navigation             │ Data Access            │
        ▼                        ▼                        ▼
┌───────────────┐        ┌───────────────┐        ┌───────────────┐
│   Frame       │        │  Repositories │        │  Hardware     │
│ NavigationSvc │        │               │        │               │
└───────────────┘        │ • Generic     │        │ • Printer     │
                         │ • UnitOfWork  │        │ • Scanner     │
                         └───────┬───────┘        │ • Drawer      │
                                 │                └───────────────┘
                                 │ EF Core
                                 ▼
                         ┌───────────────┐
                         │  AppDbContext │
                         │               │
                         │ • DbSets      │
                         │ • Migrations  │
                         └───────┬───────┘
                                 │
                                 ▼
                         ┌───────────────┐
                         │   Database    │
                         │  SQLite/SQL   │
                         └───────────────┘
```

---

**Visual Documentation Complete!**

These diagrams provide a comprehensive visual understanding of:

- System architecture
- Data flow
- Component interactions
- Security layers
- MVVM pattern implementation

Version: 1.0.0  
Last Updated: February 2026
