# 🔨 Build & Deployment Guide

## Development Setup

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Visual Studio 2022** (Community or higher) OR **VS Code**
- **Git** (optional)

### Clone & Build

```powershell
# Navigate to project directory
cd F:\Raw\kasher

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Build specific configuration
dotnet build -c Release
```

---

## 🎨 UI Theme (Al‑Atmani 2026 + optional legacy)

- Default UI is **Al‑Atmani 2026**.
- Legacy `Themes/SpaceTheme.xaml` is **disabled by default** to avoid resource collisions/visual bleed.
- Enable only when needed via `src/SmartPOS.WPF/appsettings.json`:

```json
{
  "Ui": {
    "EnableLegacySpaceTheme": true
  }
}
```

---

## Database Setup

### SQLite (Default - Development)

```powershell
# Navigate to Infrastructure project
cd src\SmartPOS.Infrastructure

# Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ..\SmartPOS.WPF

# Apply migration (creates database)
dotnet ef database update --startup-project ..\SmartPOS.WPF
```

**Result**: Runtime DB is created in `%LocalAppData%\SmartPOS\smartpos.db` (unless `SMARTPOS_DB_PATH` is set).

### SQLite Notes (Aggregations)

SQLite can be strict about `SUM()` over `decimal` projections. In reporting/shift/dashboard paths, totals are aggregated safely (sum as `double?` in SQL, then cast back to `decimal`).

### SQL Server (Production)

1. **Update appsettings.json**:

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SmartPOS;Trusted_Connection=True;"
  }
}
```

2. **Update App.xaml.cs** (line 24):

```csharp
// Optional provider switch (current runtime default remains SQLite)
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        context.Configuration.GetConnectionString("DefaultConnection")));
```

3. **Run migrations**:

```powershell
dotnet ef database update --startup-project ..\SmartPOS.WPF
```

---

## Running the Application

### Development Mode

```powershell
cd src\SmartPOS.WPF
dotnet run
```

**Or** press `F5` in Visual Studio

### Debug Mode

```powershell
dotnet run --configuration Debug
```

### Release Mode

```powershell
dotnet run --configuration Release
```

---

## Testing

### Manual Testing Checklist

#### ✅ POS Module

- [ ] Add product by barcode
- [ ] Increase/decrease quantity
- [ ] Apply discount
- [ ] Multiple payment methods
- [ ] Print receipt (if printer available)
- [ ] Open cash drawer
- [ ] Hold sale
- [ ] Complete sale
- [ ] Clear cart

#### ✅ Dashboard

- [ ] View today's sales
- [ ] View profit calculation
- [ ] Check low stock alerts
- [ ] Recent transactions display

#### ✅ Printing

- [ ] Printing uses a real printer (not OneNote/PDF/XPS)
- [ ] Saved printer name resolves correctly

#### ✅ Database

- [ ] Product CRUD operations
- [ ] Sale records creation
- [ ] Stock movement tracking
- [ ] Expense recording

### Test Data

Run this after first build to add sample data:

```sql
-- Add sample category
INSERT INTO Categories (Name, Description, ColorCode, IsActive, CreatedAt)
VALUES ('Electronics', 'Electronic items', '#2196F3', 1, datetime('now'));

-- Add sample product
INSERT INTO Products (Barcode, Name, PurchasePrice, SellingPrice, Stock, MinStockLevel, Unit, CategoryId, IsActive, CreatedAt)
VALUES ('1234567890123', 'Sample Product', 10.00, 15.00, 100, 10, 1, 1, 1, datetime('now'));
```

---

## Publishing

### Method 1: Self-Contained (Recommended)

Includes .NET runtime - no installation required on target machine.

```powershell
cd src\SmartPOS.WPF

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

**Output**: `bin\Release\net8.0-windows\win-x64\publish\`

**Pros**:

- ✅ No .NET required on target PC
- ✅ Single executable file
- ✅ Portable

**Cons**:

- ❌ Larger file size (~100MB)

### Method 2: Framework-Dependent

Requires .NET 8 runtime on target machine.

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

**Pros**:

- ✅ Smaller file size (~10MB)

**Cons**:

- ❌ Requires .NET 8 runtime installation

### Method 3: Portable (Any CPU)

```powershell
dotnet publish -c Release
```

---

## Creating an Installer

### Option 1: WiX Toolset (Free)

1. **Install WiX Toolset**: [Download](https://wixtoolset.org/)

2. **Install WiX Extension for VS**: Tools > Extensions > Search "WiX"

3. **Add WiX Setup Project** to solution

4. **Configure installer**:
   - Application name: Smart POS
   - Manufacturer: Your Company
   - Version: 1.10
   - Install location: `C:\Program Files\SmartPOS`

5. **Build installer**: Right-click WiX project > Build

**Output**: `SmartPOS.msi`

### Option 2: Inno Setup (Free, Easy)

1. **Download**: [Inno Setup](https://jrsoftware.org/isinfo.php)

2. **Create script** (`setup.iss`):

```ini
[Setup]
AppName=Smart POS
AppVersion=1.10
DefaultDirName={pf}\SmartPOS
DefaultGroupName=Smart POS
OutputDir=installer
OutputBaseFilename=SmartPOS_Setup

[Files]
Source: "src\SmartPOS.WPF\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs

[Icons]
Name: "{group}\Smart POS"; Filename: "{app}\SmartPOS.WPF.exe"
Name: "{commondesktop}\Smart POS"; Filename: "{app}\SmartPOS.WPF.exe"

[Run]
Filename: "{app}\SmartPOS.WPF.exe"; Description: "Launch Smart POS"; Flags: postinstall nowait skipifsilent
```

3. **Compile**: Right-click script > Compile

**Output**: `installer\SmartPOS_Setup.exe`

### Option 3: Advanced Installer (Commercial)

Professional installer with GUI designer.

---

## Deployment Checklist

### Pre-Deployment

- [ ] Update version number in `AssemblyInfo.cs`
- [ ] Test all features thoroughly
- [ ] Backup current database
- [ ] Update `appsettings.json` with production values
- [ ] Remove test data
- [ ] Change default admin password
- [ ] Test on clean Windows VM

### Production Configuration

**appsettings.json**:

```json
{
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=PRODUCTION_SERVER;Database=SmartPOS_Production;Trusted_Connection=True;"
  },
  "AppSettings": {
    "StoreName": "ACTUAL STORE NAME",
    "StoreAddress": "ACTUAL ADDRESS",
    "Phone": "ACTUAL PHONE",
    "Email": "ACTUAL EMAIL"
  }
}
```

### Installation Steps

1. **Install on target PC**:
   - Run installer (.msi or .exe)
   - Or copy published files to `C:\Program Files\SmartPOS`

2. **First-time setup**:
   - Launch application
   - Database auto-creates
   - Login with admin credentials
   - Change default password
   - Configure store settings

3. **Configure hardware**:
   - Connect thermal printer
   - Test print
   - Connect barcode scanner
   - Test scanning

4. **Add initial data**:
   - Create categories
   - Import products
   - Add suppliers
   - Set up users

---

## Updates & Maintenance

### Applying Updates

#### Method 1: Reinstall

1. Backup `smartpos.db`
2. Uninstall old version
3. Install new version
4. Restore `smartpos.db`

#### Method 2: In-Place Update

1. Backup `smartpos.db`
2. Close application
3. Copy new files to installation directory
4. Overwrite existing files (except `smartpos.db` and `appsettings.json`)
5. Restart application

### Database Migrations

If schema changes are required:

```powershell
# Create migration
dotnet ef migrations add UpdateV2 --startup-project ..\SmartPOS.WPF

# Apply to production
dotnet ef database update --startup-project ..\SmartPOS.WPF
```

---

## Troubleshooting Deployment

### Application Won't Start

**Error**: "The application requires .NET 8"

- **Solution**: Install .NET 8 Runtime OR use self-contained build

**Error**: "Database cannot be opened"

- **Solution**:
  1. Check file permissions
  2. Ensure SQLite DLL is present
  3. For SQL Server, check connection string

### Printer Issues

**Issue**: Printer not detected

- Check printer is powered on
- Verify USB connection
- Install printer drivers
- Set printer to RAW mode

**Issue**: Nothing prints

- Test print from Windows
- Check ESC/POS support
- Verify printer name in settings

### Performance Issues

**Issue**: Slow startup

- Optimize database indexes
- Consider SQL Server instead of SQLite
- Add caching layer

**Issue**: Slow sales processing

- Check database size
- Archive old transactions
- Optimize queries

---

## Backup Strategy

### Manual Backup

```powershell
# Backup SQLite database
copy "C:\Program Files\SmartPOS\smartpos.db" "C:\Backups\smartpos_backup_%date%.db"

# Backup SQL Server database
sqlcmd -S SERVER -Q "BACKUP DATABASE SmartPOS TO DISK='C:\Backups\SmartPOS.bak'"
```

### Automated Backup (Windows Task Scheduler)

1. Open Task Scheduler
2. Create Basic Task
3. Name: "SmartPOS Daily Backup"
4. Trigger: Daily at 11:59 PM
5. Action: Start a Program
6. Program: `powershell.exe`
7. Arguments:

```powershell
Copy-Item "C:\Program Files\SmartPOS\smartpos.db" -Destination "C:\Backups\smartpos_$(Get-Date -Format 'yyyyMMdd').db"
```

---

## Monitoring

### Log Files

Application logs are stored in:

- `C:\Program Files\SmartPOS\Logs\`

Check for:

- Error messages
- Failed transactions
- Database connection issues

### Health Checks

Daily checks:

- [ ] Database backup successful
- [ ] Printer responding
- [ ] No error logs
- [ ] Disk space adequate (>5GB free)

Weekly checks:

- [ ] Review Z-Reports
- [ ] Check low stock items
- [ ] Verify data integrity

---

## Rollback Plan

If update fails:

1. **Stop application**
2. **Restore database backup**:
   ```powershell
   copy "C:\Backups\smartpos_backup_YYYYMMDD.db" "C:\Program Files\SmartPOS\smartpos.db"
   ```
3. **Reinstall previous version**
4. **Verify functionality**

---

## Multi-Store Deployment

For multiple locations:

### Central Database Approach

1. Set up SQL Server on central server
2. Configure each store to connect to central database
3. Use VPN for secure connection

**Connection String**:

```json
"DefaultConnection": "Server=CENTRAL_SERVER;Database=SmartPOS;User Id=pos_user;Password=SECURE_PASSWORD;"
```

### Distributed Approach

1. Each store has local database
2. Implement sync service (future enhancement)
3. Aggregate reports centrally

---

## Security Best Practices

### Application Security

- [ ] Change default admin password
- [ ] Create individual user accounts
- [ ] Assign appropriate roles
- [ ] Enable Windows Firewall
- [ ] Use strong database passwords

### Database Security

- [ ] Regular backups
- [ ] Encrypted connections (SQL Server)
- [ ] Restrict database file permissions
- [ ] Use parameterized queries (already implemented)

### Physical Security

- [ ] Secure server room
- [ ] Backup offsite storage
- [ ] UPS for power protection

---

## Performance Optimization

### Database Optimization

```sql
-- Add indexes for frequently queried fields
CREATE INDEX IX_Products_Barcode ON Products(Barcode);
CREATE INDEX IX_Sales_SaleDate ON Sales(SaleDate);
CREATE INDEX IX_SaleDetails_ProductId ON SaleDetails(ProductId);
```

### Application Optimization

- Enable lazy loading selectively
- Use projection for large datasets
- Implement caching for static data (categories)
- Archive old transactions yearly

---

## License Activation (Future)

Placeholder for licensing system:

1. Generate unique machine ID
2. Send to license server
3. Receive activation key
4. Store encrypted in registry
5. Validate on startup

---

## Support & Documentation

### User Training

- Provide `QUICKSTART.md` to staff
- Train on keyboard shortcuts
- Practice with test data
- Demonstrate error handling

### Technical Support

- Maintain changelog
- Document known issues
- Provide update notifications
- Remote assistance via TeamViewer

---

**Deployment Checklist Complete!**

Version: 1.10  
Last Updated: February 2026

---

## Quick Reference

### Build Commands

```powershell
dotnet restore                          # Restore packages
dotnet build                           # Build Debug
dotnet build -c Release                # Build Release
dotnet publish -c Release -r win-x64   # Publish
```

### Database Commands

```powershell
dotnet ef migrations add NAME          # Create migration
dotnet ef database update              # Apply migrations
dotnet ef database drop                # Reset database
```

### Run Commands

```powershell
dotnet run                             # Run Debug
dotnet run -c Release                  # Run Release
```

---

**Ready for Production Deployment! 🚀**
