using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

// Unlike the "suggest, don't lock" resolvers elsewhere in Sales, a CreditLimit is an explicit hard
// gate the user opted into by setting it - Customer.CreditLimit is null (no limit enforced) by
// default. Mirrors Pages/Customers/Detail.cshtml.cs's own OutstandingBalance computation (sum of
// unpaid Invoice Total - AmountPaid, ignoring Cancelled invoices) so the number shown to sales
// staff matches the number the gate enforces.
public static class CreditCheck
{
    public static async Task EnsureWithinLimitAsync(
        IRepository<Customer, Guid> customerRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        Guid customerId,
        decimal additionalAmount)
    {
        var customer = await customerRepository.FindAsync(customerId);
        if (customer?.CreditLimit is not { } creditLimit)
        {
            return;
        }

        var invoices = await invoiceRepository.GetListAsync(x => x.CustomerId == customerId && x.Status != InvoiceStatus.Cancelled);
        var invoiceIds = invoices.Select(x => x.Id).ToList();

        decimal outstanding = 0;
        if (invoiceIds.Count > 0)
        {
            var lines = await invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
            var linesByInvoiceId = lines.ToLookup(x => x.InvoiceId);

            var payments = await paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
            var paidByInvoiceId = payments.ToLookup(x => x.InvoiceId).ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            outstanding = invoices.Sum(invoice =>
            {
                var total = linesByInvoiceId[invoice.Id].Sum(x => x.Total());
                var paid = paidByInvoiceId.GetValueOrDefault(invoice.Id);
                return Math.Max(0, total - paid);
            });
        }

        if (outstanding + additionalAmount > creditLimit)
        {
            throw new UserFriendlyException(
                $"This would put {customer.Name} at {(outstanding + additionalAmount):N2} outstanding, over their credit limit of {creditLimit:N2} ({outstanding:N2} already outstanding).");
        }
    }
}