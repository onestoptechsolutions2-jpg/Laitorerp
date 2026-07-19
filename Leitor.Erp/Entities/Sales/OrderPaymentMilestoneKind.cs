namespace Leitor.Erp.Entities.Sales;

public enum OrderPaymentMilestoneKind
{
    // The default for anything a staff member adds by hand via the Order Detail page.
    Progress = 0,

    // Auto-created by OrderAppService when Order.Status transitions to Confirmed for a
    // Milestone-terms order - see OrderAppService.OnOrderConfirmedAsync.
    Deposit = 1,

    // Marks which milestone gates the final invoice: OrderAppService.ConvertMilestoneToInvoiceAsync
    // requires every FieldServiceJob linked to the order to be Completed before invoicing a
    // milestone of this Kind.
    Final = 2
}
