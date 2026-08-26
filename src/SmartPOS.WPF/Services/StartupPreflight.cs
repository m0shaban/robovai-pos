using System.IO;
using System.Reflection;

namespace SmartPOS.WPF.Services;

internal static class StartupPreflight
{
    private static readonly string[] CriticalAssemblyFiles =
    {
        "SmartPOS.Infrastructure.dll",
        "Microsoft.EntityFrameworkCore.dll",
        "Microsoft.EntityFrameworkCore.Relational.dll",
        "Microsoft.EntityFrameworkCore.Sqlite.dll",
        "Microsoft.Data.Sqlite.dll"
    };

    public static void ValidateOrThrow()
    {
        var baseDir = AppContext.BaseDirectory;
        var missingFiles = CriticalAssemblyFiles
            .Where(file => !File.Exists(Path.Combine(baseDir, file)))
            .ToArray();

        WriteReport(baseDir, missingFiles);

        if (missingFiles.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Startup preflight failed. The local runtime output is incomplete. " +
            $"Missing files: {string.Join(", ", missingFiles)}");
    }

    private static void WriteReport(string baseDir, string[] missingFiles)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RoboVAI",
            "SmartPOS");

        Directory.CreateDirectory(logDir);

        var lines = new List<string>
        {
            $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"BaseDir: {baseDir}",
            $"EntryAssembly: {Assembly.GetEntryAssembly()?.FullName}",
            $"ProcessPath: {Environment.ProcessPath}",
            $"CurrentDirectory: {Environment.CurrentDirectory}",
            $"DatabasePath: {SmartPOS.Infrastructure.Data.DatabasePathHelper.GetDatabasePath()}",
            $"MissingFiles: {(missingFiles.Length == 0 ? "None" : string.Join(", ", missingFiles))}"
        };

        File.WriteAllLines(Path.Combine(logDir, "startup_preflight.log"), lines);
    }
}
