using System.IO;
using Microsoft.Data.Sqlite;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

public class BackupService : IBackupService
{
    private static DateTime _lastAutoBackupDate = DateTime.MinValue;

    public async Task<string> CreateBackupAsync(string destinationFolder)
    {
        return await Task.Run(() =>
        {
            var dbPath = DatabasePathHelper.GetDatabasePath();
            if (!File.Exists(dbPath))
                throw new FileNotFoundException("Database file not found.", dbPath);

            Directory.CreateDirectory(destinationFolder);
            var fileName = $"Backup_SmartPOS_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";
            var backupPath = Path.Combine(destinationFolder, fileName);

            // Flush WAL before copying
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(FULL);";
                cmd.ExecuteNonQuery();
            }
            SqliteConnection.ClearAllPools();

            File.Copy(dbPath, backupPath, overwrite: true);
            var walPath = dbPath + "-wal";
            var shmPath = dbPath + "-shm";
            if (File.Exists(walPath)) File.Copy(walPath, backupPath + "-wal", true);
            if (File.Exists(shmPath)) File.Copy(shmPath, backupPath + "-shm", true);

            return backupPath;
        });
    }

    public async Task<string> RunAutoBackupIfDueAsync(string destinationFolder, int maxBackupCount)
    {
        if (_lastAutoBackupDate.Date == DateTime.Today)
            return string.Empty; // already backed up today

        var backupPath = await CreateBackupAsync(destinationFolder);
        _lastAutoBackupDate = DateTime.Today;

        // Cleanup old backups — keep only last N
        await Task.Run(() =>
        {
            if (maxBackupCount <= 0) return;
            var files = Directory.GetFiles(destinationFolder, "Backup_SmartPOS_*.db")
                .OrderByDescending(f => f)
                .Skip(maxBackupCount)
                .ToList();
            foreach (var f in files)
            {
                try { File.Delete(f); } catch { /* ignore */ }
            }
        });

        return backupPath;
    }

    public async Task RestoreBackupAsync(string backupFilePath)
    {
        await Task.Run(async () =>
        {
            if (!File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found.", backupFilePath);

            var dbPath = DatabasePathHelper.GetDatabasePath();
            SqliteConnection.ClearAllPools();
            await Task.Delay(100);

            var tempBackup = dbPath + ".bak";
            if (File.Exists(dbPath)) File.Copy(dbPath, tempBackup, true);

            try
            {
                File.Copy(backupFilePath, dbPath, overwrite: true);
            }
            catch
            {
                if (File.Exists(tempBackup)) File.Copy(tempBackup, dbPath, true);
                throw;
            }
            finally
            {
                if (File.Exists(tempBackup)) File.Delete(tempBackup);
            }
        });
    }
}
