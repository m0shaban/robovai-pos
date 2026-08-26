using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Repositories;
using SmartPOS.Infrastructure.Services;
using SmartPOS.WPF.Services;
using SmartPOS.WPF.Views;
using System.Windows;
using System.Windows.Threading;
using SmartPOS.WPF.Debug;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace SmartPOS.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    public IHost Host => _host ?? throw new InvalidOperationException("Application host has not been initialized yet.");

    public App()
    {
        // #region agent log
        AgentDebugLog.Write("pre-fix", "H3", "App.xaml.cs:App()", "App constructor entered");
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            AgentDebugLog.Write("pre-fix", "H2", "App.xaml.cs:AssemblyResolve", "AssemblyResolve", new { name = args.Name, req = args.RequestingAssembly?.FullName });
            return StartupDiagnostics.TryResolveFromBaseDirectory(args.Name, "pre-fix", "App.xaml.cs:AssemblyResolve");
        };
        // #endregion agent log

        // Predictive crash visibility: show unhandled UI exceptions instead of silently exiting.
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                MessageBox.Show(
                    $"Unhandled UI Error: {args.Exception.Message}\n\nStack Trace:\n{args.Exception}",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                args.Handled = true;
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"Unhandled App Error: {ex.Message}\n\nStack Trace:\n{ex}",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            MessageBox.Show(
                $"Unobserved Task Error: {args.Exception.GetBaseException().Message}\n\nDetails:\n{args.Exception}",
                "Critical Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.SetObserved();
        };

    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            StartupPreflight.ValidateOrThrow();

            // Initialize LiveCharts with SkiaSharp renderer
            LiveCharts.Configure(config => config.AddSkiaSharp());

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Keep app alive while dialogs are shown
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _host = BuildHost();

            await _host.StartAsync();

            ApplyOptionalThemes();

            // 1. Check Activation First
            {
                var licenseService = _host.Services.GetRequiredService<ILicenseService>();
                // Automatically start 14-day free trial if fresh installation
                var status = await licenseService.StartTrialAsync();

                if (!status.IsValid)
                {
                    if (status.IsInGrace)
                    {
                        MessageBox.Show($"تنبيه: انتهى التفعيل. متبقي {status.DaysRemaining} يوم مهلة.\nرقم الجهاز: {licenseService.GetDeviceId()}", "تنبيه التفعيل", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        var activationWindow = _host.Services.GetRequiredService<ActivationWindow>();
                        if (activationWindow.ShowDialog() != true)
                        {
                            Shutdown();
                            return;
                        }

                        // Re-verify after activation
                        status = await licenseService.GetStatusAsync();
                        if (!status.IsValid && !status.IsInGrace)
                        {
                            MessageBox.Show("لم يتم تفعيل البرنامج بشكل صحيح.", "تفعيل", MessageBoxButton.OK, MessageBoxImage.Error);
                            Shutdown();
                            return;
                        }
                    }
                }
            }

            // 2. Initialize Database (create schema + seed data)
            {
                var dbPath = DatabasePathHelper.GetDatabasePath();
                var connectionString = DatabasePathHelper.GetConnectionString();
                AgentDebugLog.Write("DB_INIT", "INFO", "DatabasePathUsed", dbPath);

                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
                using var initContext = new AppDbContext(optionsBuilder.Options);

                await DbInitializer.InitializeAsync(initContext);

                // Auto-backup to Desktop once per day to protect from hidden-folder data loss
                await TryAutoBackupAsync();
            }

            // Load settings into cache
            var settingsService = _host.Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadSettingsAsync();

            // 3. First-Time Setup Wizard if fresh installation / not yet configured
            if (!settingsService.IsFirstRunCompleted)
            {
                var setupWizard = _host.Services.GetRequiredService<FirstTimeSetupWindow>();
                setupWizard.ShowDialog();
                // Reload settings after wizard completion
                await settingsService.LoadSettingsAsync();
            }

            // Initialize Theme & UI Scaling
            var themeService = _host.Services.GetRequiredService<SmartPOS.WPF.Services.IThemeService>();
            themeService.Initialize();

            // Smart Auto-Backup: use settings-configured folder and count
            if (settingsService.AutoBackupEnabled)
            {
                var backupService = _host.Services.GetRequiredService<IBackupService>();
                _ = backupService.RunAutoBackupIfDueAsync(settingsService.BackupFolder, settingsService.MaxBackupCount);
            }

            // Configure barcode scanner from saved settings
            var barcodeService = _host.Services.GetRequiredService<IBarcodeService>();
            barcodeService.Configure(
                settingsService.BarcodeMode,
                settingsService.BarcodeCOMPort,
                settingsService.BarcodeBaudRate,
                settingsService.BarcodeTimeoutMs);

            // 4. Show Login Window
            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            // Write crash log to LocalAppData (Program Files is read-only)
            var logDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RoboVAI", "SmartPOS");
            System.IO.Directory.CreateDirectory(logDir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(logDir, "fatal_startup_error.log"), ex.ToString());
            MessageBox.Show($"Startup Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static IHost BuildHost()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                var exeDir = AppContext.BaseDirectory;
                config.SetBasePath(exeDir);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    var connectionString = DatabasePathHelper.GetConnectionString();
                    options.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"))
                           // Apply WAL mode PRAGMAs on every new SQLite connection for stability
                           .AddInterceptors(new SmartPOS.Infrastructure.Interceptors.SqliteWalInterceptor());
                });

                services.AddHostedService<GcCompactionService>();
                // LAN HTTP Server: enables the PWA (mobile/web) to sync data over local Wi-Fi
                services.AddSingleton<SmartPOS.Infrastructure.Services.LanHttpServerService>();
                services.AddHostedService(sp => sp.GetRequiredService<SmartPOS.Infrastructure.Services.LanHttpServerService>());
                // Cloud Sync Service: auto-syncs telemetry & sales to Executive Cloud Backend
                services.AddHostedService<SmartPOS.Infrastructure.Services.CloudSyncService>();
                // Telegram Bot Service: sends real-time Telegram alerts to store owner on sales & Z-reports
                services.AddSingleton<SmartPOS.Infrastructure.Services.TelegramBotService>();
                services.AddHostedService(sp => sp.GetRequiredService<SmartPOS.Infrastructure.Services.TelegramBotService>());

                services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                services.AddTransient<IShiftRepository, ShiftRepository>();
                services.AddTransient<IUnitOfWork, UnitOfWork>();

                services.AddSingleton<IPrintingService, PrintingService>();
                services.AddSingleton<IReportService, ReportService>();
                services.AddSingleton<IBarcodeService, BarcodeService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IBackupService, BackupService>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<ILicenseService, LicenseService>();
                services.AddSingleton<SmartPOS.Core.Interfaces.IThemeService, SmartPOS.WPF.Services.ThemeService>();
                services.AddSingleton<SmartPOS.WPF.Services.IThemeService>(sp => (SmartPOS.WPF.Services.IThemeService)sp.GetRequiredService<SmartPOS.Core.Interfaces.IThemeService>());
                services.AddSingleton<ISoundService, SmartPOS.WPF.Services.SoundService>();
                services.AddTransient<IAuthorizationService, AuthorizationService>();

                services.AddTransient<MainPOSViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ProductsViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<ExpensesViewModel>();
                services.AddTransient<CustomersViewModel>();
                services.AddTransient<CategoriesViewModel>();
                services.AddTransient<InvoicesViewModel>();
                services.AddTransient<ShiftManagementViewModel>();
                services.AddTransient<LoyaltyViewModel>();
                services.AddTransient<ReturnsViewModel>();
                services.AddTransient<SuppliersViewModel>();
                services.AddTransient<PurchaseOrdersViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<UsersViewModel>();
                services.AddTransient<AuditLogViewModel>();
                services.AddTransient<RentalsViewModel>();
                services.AddTransient<StockAuditViewModel>();
                services.AddTransient<StockAuditPage>();

                services.AddSingleton<IUserService, CurrentUserService>();
                services.AddTransient<User>(sp => sp.GetRequiredService<IUserService>().CurrentUser!);

                services.AddTransient<LoginViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<ActivationWindow>();
                services.AddTransient<FirstTimeSetupWindow>();
            })
            .Build();
    }

    /// <summary>
    /// Silently backs up the database to the Desktop once per day.
    /// Protects against the hidden-folder problem: if Windows crashes,
    /// the user always has a recoverable copy on their Desktop.
    /// </summary>
    private static async Task TryAutoBackupAsync()
    {
        try
        {
            var dbPath = SmartPOS.Infrastructure.Data.DatabasePathHelper.GetDatabasePath();
            if (!System.IO.File.Exists(dbPath)) return;

            var appDataDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RoboVAI", "SmartPOS");

            var stampFile = System.IO.Path.Combine(appDataDir, "last_auto_backup.txt");
            var today = DateTime.Today.ToString("yyyy-MM-dd");

            // Only backup once per day
            if (System.IO.File.Exists(stampFile) &&
                System.IO.File.ReadAllText(stampFile).Trim() == today)
                return;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var backupFolder = System.IO.Path.Combine(desktop, "RoboVAI POS - نسخ احتياطية");
            System.IO.Directory.CreateDirectory(backupFolder);

            // Keep only last 7 daily backups on Desktop
            var backupName = $"SmartPOS_AutoBackup_{today}.db";
            var backupPath = System.IO.Path.Combine(backupFolder, backupName);

            await Task.Run(() =>
            {
                System.IO.File.Copy(dbPath, backupPath, overwrite: true);

                // Clean backups older than 7 days
                foreach (var f in System.IO.Directory.GetFiles(backupFolder, "SmartPOS_AutoBackup_*.db"))
                {
                    var fi = new System.IO.FileInfo(f);
                    if (fi.LastWriteTime < DateTime.Today.AddDays(-7))
                        fi.Delete();
                }
            });

            // Record today's stamp
            await System.IO.File.WriteAllTextAsync(stampFile, today);
        }
        catch
        {
            // Auto-backup is silent — never block startup on failure
        }
    }

    private void ApplyOptionalThemes()
    {
        if (_host == null)
        {
            return;
        }

        var configuration = _host.Services.GetService<IConfiguration>();
        if (configuration == null)
        {
            return;
        }

        var enableLegacySpaceTheme = configuration.GetValue("Ui:EnableLegacySpaceTheme", false);
        if (!enableLegacySpaceTheme)
        {
            return;
        }

        // Load legacy SpaceTheme only when explicitly enabled to avoid resource collisions / visual bleed.
        var mergedDictionaries = Resources?.MergedDictionaries;
        if (mergedDictionaries == null)
        {
            return;
        }

        var spaceThemeSource = new Uri("Themes/SpaceTheme.xaml", UriKind.Relative);
        if (mergedDictionaries.Any(d => d.Source != null && d.Source.Equals(spaceThemeSource)))
        {
            return;
        }

        mergedDictionaries.Add(new ResourceDictionary { Source = spaceThemeSource });
    }
}
