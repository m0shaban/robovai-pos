# PROJECT_OVERVIEW.md — RobovAI PRO POS

> Honest overview of the system: what it is, who it is for, and what is actually production-ready.

---

## What the System Is

RobovAI PRO POS is a dual-mode point-of-sale and inventory management platform that ships in two independent but interoperable products:

| Component                   | Tech                               | Platform          | Deployment                             |
| --------------------------- | ---------------------------------- | ----------------- | -------------------------------------- |
| **SmartPOS WPF**            | C# .NET 8, WPF, EF Core / SQLite   | Windows desktop   | InnoSetup installer (`.exe`)           |
| **Smart Inventory Pro WMS** | Vanilla JS PWA, Firebase, Dexie.js | Any browser (PWA) | GitHub Pages — `pos.robovai.tech/wms/` |

---

## Use Cases

### 1. WPF Alone — Offline Retail Cashier

**Who**: Small shop owner, single-lane counter, no internet required.

**What it does**:

- Full POS transaction flow: add product → calculate total → accept cash/card → print receipt
- Local SQLite database (no cloud dependency)
- Product catalog management, category hierarchy
- Basic sales reports with charts (LiveCharts)
- Customer purchase history
- Barcode scanning via USB barcode reader (or camera via OpenCvSharp4)
- Arabic + English UI, RTL layouts in MaterialDesign

**Deployment**: One `.exe` installer, self-contained, runs on Windows 10/11 x64. No .NET runtime required on target machine.

**Editions shipped**:

- **Platinum** — full-featured, premium branding (`wizard_small.bmp`)
- **Kaf5** — custom/branded build for a specific client (`wizard_small_kaf5.bmp`)

---

### 2. WMS Alone — Warehouse / Inventory PWA

**Who**: Warehouse supervisor or multi-user team that needs browser-based access.

**What it does**:

- Full Arabic RTL UI, offline-first (IndexedDB via Dexie.js)
- Products, categories, suppliers, customers, purchase orders, invoices
- Inventory adjustments, stock movements, low-stock alerts
- Expense tracking
- Charts and dashboard (Chart.js)
- PDF export (jsPDF + autotable), Excel export (xlsx), barcode generation (JsBarcode)
- User management with roles (admin / supervisor / worker)
- Firebase Firestore sync for multi-user real-time collaboration
- Installable as PWA (service worker, manifest)

**Deployment**: Hosted on GitHub Pages, accessible from any device at `pos.robovai.tech/wms/`.

---

### 3. WPF + WMS Together — Full Ecosystem

**Who**: Retailer with a permanent desktop cashier station AND staff managing warehouse from tablets or phones.

**Integration mechanism — QR Auth Pairing**:

1. WMS admin opens Settings → User Management → clicks 🔲 next to a user
2. WMS generates a signed QR code payload (`qr-auth-v1`, HMAC-SHA256)
3. WPF cashier opens POS login → selects "QR Login" → points camera at the screen
4. WPF decodes QR, verifies HMAC signature, logs in the matching user

**What this enables**:

- WMS is the user directory; WPF authenticates via QR — no separate credential entry
- Cashier uses WPF offline; manager monitors stock from browser
- Future: bidirectional Firebase sync between WPF sales and WMS inventory

---

## Business Model

| Aspect                | Current approach                                                         |
| --------------------- | ------------------------------------------------------------------------ |
| **License model**     | Perpetual one-time sale per installation (no SaaS recurring)             |
| **Delivery**          | InnoSetup `.exe` installer distributed directly to client                |
| **Editions**          | Platinum (full) and client-branded custom builds                         |
| **WMS hosting**       | Free — GitHub Pages + Firebase free tier (Spark plan)                    |
| **Support / updates** | Manual — new installer delivered by developer                            |
| **Target market**     | Arabic-speaking SMB retail and wholesale (GCC + Egypt)                   |
| **Revenue driver**    | Per-client custom builds + initial sale; no recurring billing system yet |

---

## Production Readiness — Honest Audit

### ✅ What is production-ready

| Area                        | Status                                                           |
| --------------------------- | ---------------------------------------------------------------- |
| WPF offline POS             | Ready — SQLite persists, no network dependency                   |
| WMS PWA offline             | Ready — Dexie.js IndexedDB, service worker, works offline        |
| WMS Firebase sync           | Ready for small teams — Firestore real-time, auth rules in place |
| QR Auth pairing             | Fixed and deployed — WPF deeplink + WMS signed QR generation     |
| Arabic RTL UI               | Both products are fully Arabic RTL                               |
| Installer (Platinum & Kaf5) | Ready — InnoSetup v6.7.1, self-contained, signed metadata        |
| Landing page                | Live at `pos.robovai.tech` via GitHub Pages                      |

---

### ⚠️ What is incomplete or fragile

| Area                               | Issue                                                                                                                                                                                           |
| ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **WPF ↔ WMS real-time sync**       | No bidirectional data sync implemented yet. WPF writes locally to SQLite; WMS writes to Firebase. There is NO bridge between them beyond the QR login. A sale on WPF does not reduce WMS stock. |
| **WPF payment processing**         | Cash and card totals are calculated but there is no real card terminal integration (no POS device SDK, no acquirer API).                                                                        |
| **WMS user authentication**        | Firebase Auth is used for sign-in; the WMS QR role system is application-level, not enforced by Firestore security rules for every collection.                                                  |
| **Firestore security rules**       | No evidence of hardened Firestore rules that enforce per-user data isolation across all collections. A determined user could read other tenants' data if rules are permissive.                  |
| **Multi-tenancy**                  | WMS is effectively single-tenant per Firebase project. Different businesses would share one Firestore DB unless separate projects are provisioned per client.                                   |
| **WPF cloud backup**               | SQLite DB lives only on the cashier machine. No cloud backup or remote recovery mechanism.                                                                                                      |
| **WPF receipt printer**            | Receipt generation code exists but print integration is basic — no ESC/POS thermal printer driver, only Windows GDI print dialog.                                                               |
| **License/activation enforcement** | License key generation script exists (`generate_license_key.py`), but no active license check in the WPF runtime. The installer is not locked to a machine ID.                                  |
| **Error monitoring**               | No crash reporting (no Sentry, no Application Insights). Failures on client machines are invisible.                                                                                             |
| **Automated tests**                | Near-zero automated test coverage on both products. Playwright is a dev dependency in WMS but no test files were written.                                                                       |
| **CI/CD**                          | No automated build pipeline. Builds are manual (npm run build → copy → git push for WMS; dotnet publish + ISCC.exe for WPF).                                                                    |
| **Localization**                   | UI is Arabic-first; English strings exist in parts of WPF but the app is not fully bilingual via a proper i18n system.                                                                          |

---

### 🔴 Security items requiring attention before wider distribution

| Risk                            | Description                                                                                                                                                                                  |
| ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **QR HMAC secret**              | The HMAC secret for QR auth is stored in `localStorage` (`robovai_qr_secret`). If an attacker can read localStorage (XSS, physical access), they can forge any user QR token.                |
| **Firebase API key in source**  | `AIzaSyDO9Z2GppWCMVx4QuFTEJyUBXBfsZ5v7xQ` is hardcoded in the WMS bundle and visible to any user. This is normal for Firebase web apps but requires tight Firestore + Auth rules to be safe. |
| **No rate limiting on QR scan** | WPF QR login has no brute-force or replay protection. A captured QR code is valid until the HMAC secret changes.                                                                             |

---

## Technology Summary

**SmartPOS WPF** (`src/SmartPOS.WPF/`)

- .NET 8, WPF, Windows 10.0.19041+, x64 only
- MaterialDesign 5.3.2, CommunityToolkit.Mvvm, EF Core 8 / SQLite
- LiveCharts (SkiaSharp), OpenCvSharp4, QRCoder
- Clean Architecture: `Core` / `Application` / `Infrastructure` / `WPF`

**Smart Inventory Pro WMS** (`smart-inventory-pro/`)

- Vite 6.4.2, Vanilla JS (ES modules, no framework)
- Firebase 12 (Firestore + Auth), Dexie.js 4 (IndexedDB)
- Chart.js 4, jsPDF 3, xlsx 0.18, SweetAlert2 11, Lucide icons
- Deployed bundle: single-entry Vite build → `~2.85 MB` JS
- Version: 2.6.0
