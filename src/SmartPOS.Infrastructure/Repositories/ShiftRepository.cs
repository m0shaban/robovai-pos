using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Repositories;

public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Shift?> GetActiveShiftByUserIdAsync(int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open && !s.IsDeleted);
    }

    public async Task<bool> HasActiveShiftAsync(int userId)
    {
        return await _dbSet
            .AnyAsync(s => s.UserId == userId && s.Status == ShiftStatus.Open && !s.IsDeleted);
    }

    public async Task<decimal> GetShiftSalesTotalAsync(int shiftId)
    {
        // Performance Optimized: Aggregate in DB
        var total = await _context.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shiftId && s.Status == SaleStatus.Completed && !s.IsDeleted)
            .Select(s => (double?)s.TotalAmount)
            .SumAsync();
        return (decimal)(total ?? 0d);
    }

    public async Task<IEnumerable<Sale>> GetShiftTransactionsAsync(int shiftId)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
            .Where(s => s.ShiftId == shiftId && !s.IsDeleted)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
    }

    public async Task<decimal> CalculateExpectedBalanceAsync(int shiftId)
    {
        var shift = await GetByIdAsync(shiftId);
        if (shift == null) return 0;

        var shiftEnd = shift.EndTime ?? DateTime.Now;

        // Cash sales during the shift
        var totalCashSales = (decimal)(await _context.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shiftId && s.PaymentMethod == PaymentMethod.Cash
                     && s.Status == SaleStatus.Completed && !s.IsDeleted && s.TotalAmount > 0)
            .Select(s => (double?)s.TotalAmount)
            .SumAsync() ?? 0d);

        // Cash refunds issued during the shift (negative sales REF-*)
        var cashRefundsRaw = (decimal)(await _context.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shiftId && s.PaymentMethod == PaymentMethod.Cash
                     && s.TotalAmount < 0 && !s.IsDeleted)
            .Select(s => (double?)s.TotalAmount)
            .SumAsync() ?? 0d);
        var totalCashRefunds = Math.Abs(cashRefundsRaw);

        // Expenses paid out during the shift
        var totalExpenses = (decimal)(await _context.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == shift.UserId && e.ExpenseDate >= shift.StartTime
                     && e.ExpenseDate <= shiftEnd && !e.IsDeleted)
            .Select(e => (double?)e.Amount)
            .SumAsync() ?? 0d);

        // Expected drawer = Opening + Cash Sales - Cash Refunds - Cash Expenses
        return shift.OpeningBalance + totalCashSales - totalCashRefunds - totalExpenses;
    }

    public async Task CloseShiftAsync(int shiftId, decimal actualCash, string notes = "")
    {
        var shift = await GetByIdAsync(shiftId);
        if (shift == null) throw new ArgumentException("Shift not found");

        if (shift.Status != ShiftStatus.Open)
            throw new InvalidOperationException("Shift is already closed");

        var shiftEnd = DateTime.Now;
        shift.EndTime = shiftEnd;

        var expectedBalance = await CalculateExpectedBalanceAsync(shiftId);

        shift.ClosingBalance = actualCash;
        shift.ExpectedBalance = expectedBalance;
        shift.Difference = actualCash - expectedBalance;
        shift.Status = ShiftStatus.Closed;
        shift.Notes = notes;

        await _context.SaveChangesAsync();
    }
}
