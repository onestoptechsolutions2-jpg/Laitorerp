namespace Leitor.Erp.Entities.Sales;

public enum PaymentTerms
{
    DueOnReceipt = 0,
    Net15 = 1,
    Net30 = 2,
    Net45 = 3,
    Net60 = 4,

    // Due date is driven by each OrderPaymentMilestone instead of a single fixed offset - see
    // OrderAppService.ConvertMilestoneToInvoiceAsync.
    Milestone = 5
}
