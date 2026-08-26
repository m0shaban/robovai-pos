namespace SmartPOS.Core.Interfaces;

/// <summary>
/// Interface for report generation service
/// </summary>
public interface IReportService
{
    Task<byte[]> GenerateSalesReportPdfAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateInventoryReportPdfAsync();
    Task<byte[]> GenerateZReportPdfAsync(DateTime date);
    Task<bool> ExportToExcelAsync(string filePath, object data);
}
