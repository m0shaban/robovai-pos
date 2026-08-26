using SmartPOS.Core.Entities;

namespace SmartPOS.Core.Interfaces;

public interface IShiftRepository : IRepository<Shift>
{
    Task<Shift?> GetActiveShiftByUserIdAsync(int userId);
    Task<bool> HasActiveShiftAsync(int userId);
    Task<decimal> GetShiftSalesTotalAsync(int shiftId);
    Task CloseShiftAsync(int shiftId, decimal actualCash, string notes = "");
    Task<decimal> CalculateExpectedBalanceAsync(int shiftId);
    Task<IEnumerable<Sale>> GetShiftTransactionsAsync(int shiftId);
}
