namespace TierLedger.Domain.Invoices;

public enum PaymentMethod
{
    BankTransfer = 0,
    Card = 1,
    Cash = 2,
    Cheque = 3,
    Other = 4,
}
