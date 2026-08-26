# 🚀 Smart POS - Quick Start Guide

## UI (Al-Atmani 2026)

The app UI is dark-mode first with acrylic/glass cards, a bento-style dashboard, and a touch-first POS cashier.

Theme note (Feb 2026): Legacy `Themes/SpaceTheme.xaml` is disabled by default to avoid resource collisions/visual bleed. Enable explicitly via `src/SmartPOS.WPF/appsettings.json` → `Ui:EnableLegacySpaceTheme: true`.

## Prerequisites

- Windows 10/11
- .NET 8 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Visual Studio 2022 or VS Code
- SQL Server (optional, SQLite is default)

---

## Installation Steps

### 1️⃣ Clone or Download the Project

```bash
cd F:\Raw\kasher
```

### 2️⃣ Restore NuGet Packages

```bash
dotnet restore
```

### 3️⃣ Build the Solution

```bash
dotnet build
```

### 4️⃣ Initialize Database

```bash
cd src/SmartPOS.Infrastructure
dotnet ef database update --startup-project ../SmartPOS.WPF
```

This creates `smartpos.db` SQLite database with seed data.

### 5️⃣ Run the Application

```bash
cd ../SmartPOS.WPF
dotnet run
```

---

## First Launch

### Default Credentials

- **Username**: `admin`
- **Password**: `admin123`

**⚠️ Change default password in production!**

---

## Quick Setup Checklist

### ✅ Configure Store Information

1. Open `appsettings.json`
2. Update store details:

```json
{
  "AppSettings": {
    "StoreName": "Your Store Name",
    "StoreAddress": "Your Address",
    "Phone": "Your Phone",
    "Email": "your@email.com"
  }
}
```

### ✅ Add Categories

1. Navigate to **Products** > **Categories**
2. Click **Add Category**
3. Create categories like:
   - Electronics
   - Groceries
   - Beverages
   - Snacks

### ✅ Add Products

1. Go to **Products** > **Add Product**
2. Fill in:
   - Barcode (can use generator)
   - Product name
   - Purchase price
   - Selling price
   - Initial stock
   - Category
   - Minimum stock level

### ✅ Configure Printer (Optional)

1. Go to **Settings** > **Printer**
2. Select your thermal printer from dropdown
3. Click **Test Print**
4. If successful, enable **Auto Print**

### ✅ Setup Suppliers (Optional)

1. Navigate to **Suppliers**
2. Add your suppliers with contact info
3. Link products to suppliers

---

## Using the POS System

### 🛒 Making a Sale

1. **Navigate to POS Cashier**
   - Click "POS Cashier" in menu or press `Ctrl+1`

2. **Add Products**
   - Scan barcode with scanner, OR
   - Type barcode and press Enter, OR
   - Press F1 to focus barcode input

3. **Adjust Quantities**
   - Click `+` / `-` buttons
   - Or press F2 (increase) / F3 (decrease)

4. **Apply Discount**
   - Enter discount percentage in right panel
   - Or press F5 to apply

5. **Process Payment**
   - Select payment method (Cash/Card/Mobile)
   - Enter amount paid
   - System calculates change automatically

6. **Complete Sale**
   - Click **COMPLETE** button, OR
   - Press F9
   - Receipt prints automatically (if configured)

### ⌨️ Keyboard Shortcuts

| Key | Action                          |
| --- | ------------------------------- |
| F1  | Focus barcode input             |
| F2  | Increase selected item quantity |
| F3  | Decrease selected item quantity |
| F4  | Remove selected item            |
| F5  | Apply discount                  |
| F7  | Hold current sale               |
| F8  | Open cash drawer                |
| F9  | Complete sale                   |
| F10 | Clear cart                      |

---

## Daily Operations

### 📊 View Dashboard

- Click **Dashboard** to see:
  - Today's sales and profit
  - Transaction count
  - Low stock alerts
  - Recent sales (shown as compact cards/chips)

### 💰 End of Day (Z-Report)

1. Go to **Reports** > **Z-Report**
2. Select date (default: today)
3. Click **Generate**
4. Review sales, expenses, profit
5. Click **Print** to print on thermal printer

### 📦 Stock Management

1. Navigate to **Products**
2. View stock levels
3. Products below minimum show in red
4. Add stock via **Stock Adjustment**

### 💸 Record Expenses

1. Go to **Expenses**
2. Click **Add Expense**
3. Fill in:
   - Description
   - Amount
   - Category
   - Date
4. Save

---

## Troubleshooting

### ❌ Database Error on Startup

**Solution**:

```bash
cd src/SmartPOS.Infrastructure
dotnet ef database drop --startup-project ../SmartPOS.WPF
dotnet ef database update --startup-project ../SmartPOS.WPF
```

### ❌ Barcode Scanner Not Working

1. Test scanner in Notepad - should type characters
2. Ensure scanner is in **HID keyboard mode**
3. Check barcode format compatibility (Code 128, EAN-13, etc.)

### ❌ Printer Not Printing

1. Verify printer is **powered on**
2. Check **USB/Network connection**
3. Test print from Windows (test page)
4. Ensure printer supports **ESC/POS** commands
5. Set printer to **RAW mode** in printer settings

**Note**: The app avoids picking virtual printers (OneNote/PDF/XPS/Fax) when resolving the target printer.

### ❌ Low Performance with Large Database

Switch to SQL Server:

1. Install SQL Server
2. Update `appsettings.json`:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SmartPOS;Trusted_Connection=True;"
  }
}
```

3. Run migrations:

```bash
dotnet ef database update --startup-project ../SmartPOS.WPF
```

---

## Backup & Restore

### SQLite Database Backup

Simply copy `smartpos.db` file to safe location:

```bash
copy src\SmartPOS.WPF\smartpos.db C:\Backups\smartpos_backup_2026-02-04.db
```

### Automated Backup (Recommended)

Create scheduled task in Windows:

1. Open Task Scheduler
2. Create Basic Task
3. Set to run daily at 11:59 PM
4. Action: Start a program
5. Program: `robocopy`
6. Arguments: `F:\Raw\kasher\src\SmartPOS.WPF C:\Backups\SmartPOS smartpos.db`

---

## Next Steps

### 🎓 Learn More

- Read [ARCHITECTURE.md](ARCHITECTURE.md) for technical details
- Customize colors in `App.xaml`
- Add custom reports
- Integrate with external APIs

### 🔒 Security Hardening

- Change default admin password
- Create user accounts with appropriate roles
- Enable Windows Firewall
- Regular backups
- Keep software updated

### 📱 Advanced Features (Roadmap)

- Customer loyalty program
- Mobile app integration
- Cloud sync
- Multi-store support
- Advanced analytics

---

## Support

For issues or questions:

1. Check documentation in `ARCHITECTURE.md`
2. Review error logs in application folder
3. Contact system administrator

---

**Happy Selling! 🎉**

Version: 1.0.0  
Last Updated: February 2026
