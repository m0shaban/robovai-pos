namespace SmartPOS.Core.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string destinationFolder);
    Task<string> RunAutoBackupIfDueAsync(string destinationFolder, int maxBackupCount);
    Task RestoreBackupAsync(string backupFilePath);
}
