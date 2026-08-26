# 🔧 .NET SDK Installation Required

## ⚠️ .NET SDK Not Found

The Smart POS application requires **.NET 8 SDK** to build from source.

If you are running a published build on another machine, the **.NET 8 Runtime** (or a self-contained build) is sufficient.

---

## 📥 Installation Steps

### Method 1: Direct Download (Recommended)

1. **Download .NET 8 SDK**:
   - Visit: https://dotnet.microsoft.com/download/dotnet/8.0
   - Click **Download .NET 8.0 SDK (x64)**
   - File: ~200 MB

2. **Install**:
   - Run the downloaded installer
   - Follow the installation wizard
   - Accept default settings

3. **Verify Installation**:

   ```powershell
   dotnet --version
   # Should output: 8.0.x
   ```

4. **Restart PowerShell**:
   - Close and reopen PowerShell/VS Code terminal
   - This ensures PATH is updated

---

### Method 2: Using Winget (Windows Package Manager)

```powershell
winget install Microsoft.DotNet.SDK.8
```

---

### Method 3: Using Chocolatey

```powershell
choco install dotnet-sdk
```

---

## ✅ After Installation

Once .NET SDK is installed, run these commands:

```powershell
# Navigate to project
cd F:\Raw\kasher

# Verify .NET is installed
dotnet --version

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run application
cd src\SmartPOS.WPF
dotnet run
```

---

## 🆘 Troubleshooting

### "dotnet" not recognized after installation

**Solution**: Restart your terminal or computer

### Check if .NET is in PATH

```powershell
$env:PATH -split ';' | Select-String "dotnet"
```

### Manual PATH addition (if needed)

Add this to PATH: `C:\Program Files\dotnet\`

---

## 🎯 Quick Start After Installation

```powershell
# 1. Restore dependencies
dotnet restore

# 2. Build
dotnet build

# 3. Create database
cd src\SmartPOS.Infrastructure
dotnet ef database update --startup-project ..\SmartPOS.WPF

# 4. Run
cd ..\SmartPOS.WPF
dotnet run
```

---

## 📦 What Gets Installed

- .NET 8 SDK (~200 MB)
- .NET Runtime
- Build tools
- NuGet package manager
- Entity Framework CLI tools

---

## 🔗 Useful Links

- [.NET 8 Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [Installation Guide](https://docs.microsoft.com/dotnet/core/install/windows)

---

## ⏱️ Installation Time

- Download: ~5 minutes (depending on internet speed)
- Installation: ~2 minutes
- **Total: ~7 minutes**

---

After installing .NET SDK, you'll be ready to build and run the Smart POS application! 🚀

---

## 🎨 UI Note (Feb 2026)

- Default UI is **Al‑Atmani 2026**.
- Legacy `Themes/SpaceTheme.xaml` is optional and disabled by default.
- Enable via `src/SmartPOS.WPF/appsettings.json` → `Ui:EnableLegacySpaceTheme: true`.
