namespace Leitor.Erp.Entities.Partners;

// Calculated the moment a Commission is recorded, but never payable until its Trigger condition
// is actually satisfied - Pending -> Payable happens automatically (OnProposalAccepted: immediately
// at creation; OnClientPayment: see CommissionAutoPayableService, called from PaymentAppService the
// moment a Payment posts against the linked Invoice). Payable -> Paid is always a manual action
// (CommissionAppService.MarkPaidAsync) - the system never assumes money left the business on its own.
public enum CommissionStatus
{
    Pending = 0,
    Payable = 1,
    Paid = 2
}
