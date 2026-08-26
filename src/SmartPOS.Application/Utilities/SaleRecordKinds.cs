using SmartPOS.Core.Entities;

namespace SmartPOS.Application.Utilities;

public static class SaleRecordKinds
{
    public const string DebtPaymentPrefix = "DEF-PAY-";

    public static bool IsDebtPayment(Sale sale)
    {
        return sale.InvoiceNumber.StartsWith(DebtPaymentPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
