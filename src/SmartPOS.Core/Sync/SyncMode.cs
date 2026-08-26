namespace SmartPOS.Core.Sync;

/// <summary>
/// Defines the operational mode for the RobovAI POS &amp; WMS Sync Engine (Requirement R1).
/// Supports dynamic runtime switching without application restart.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Pure Local Intranet Mode.
    /// WPF POS hosts local Kestrel HTTP server (port 5050). All data is persisted in local SQLite/Dexie.
    /// Cloud synchronization worker is paused/disabled.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Pure Cloud-First Mode.
    /// POS/WMS connect directly to Cloud API (Vercel/Render/Firebase/PostgreSQL).
    /// Local LAN HTTP server listener is disabled.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Dual-Tier Hybrid Mode (Recommended Commercial Default).
    /// Local LAN handles ultra-fast checkout transactions.
    /// Asynchronous Outbox Worker pushes pending changes to Cloud when internet is available.
    /// Both LAN Server and Cloud Worker are active.
    /// </summary>
    Hybrid = 2
}
