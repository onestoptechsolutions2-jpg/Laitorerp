using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// GeneralLedgerReportAppService. Amounts are summed in each document's own face currency, never
// converted to base currency - same convention DashboardAppService.GetSalesStatsAsync already
// uses for OutstandingBalance, since Total/AmountPaid are always shown to a user in the document's
// own currency, not the ledger's base currency.
public class AgingReportAppService : ApplicationService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<VendorPayment, Guid> _vendorPaymentRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public AgingReportAppService(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<VendorPayment, Guid> vendorPaymentRepository,
        IRepository<Vendor, Guid> vendorRepository)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _vendorPaymentRepository = vendorPaymentRepository;
        _vendorRepository = vendorRepository;
    }

    public async Task<AgingReportDto> GetArAgingAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var invoices = await _invoiceRepository.GetListAsync(x => x.Status == InvoiceStatus.Issued && x.IssueDate <= asOfDate);
        var invoiceIds = invoices.Select(x => x.Id).ToList();

        var lines = invoiceIds.Count > 0
            ? (await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId))).ToLookup(x => x.InvoiceId)
            : Enumerable.Empty<InvoiceLine>().ToLookup(x => x.InvoiceId);
        var payments = invoiceIds.Count > 0
            ? (await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId) && x.PaymentDate <= asOfDate)).ToLookup(x => x.InvoiceId)
            : Enumerable.Empty<Payment>().ToLookup(x => x.InvoiceId);
        var namesById = (await _customerRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        var buckets = invoices
            .Select(invoice => new
            {
                invoice.CustomerId,
                AmountDue = lines[invoice.Id].Sum(x => x.Total()) - payments[invoice.Id].Sum(x => x.Amount),
                DaysOverdue = (asOfDate.Date - invoice.DueDate.Date).Days
            })
            .Where(x => x.AmountDue > 0);

        return BuildReport(asOfDate, buckets.Select(x => (x.CustomerId, x.AmountDue, x.DaysOverdue)), namesById);
    }

    public async Task<AgingReportDto> GetApAgingAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var invoices = await _supplierInvoiceRepository.GetListAsync(x => x.Status == SupplierInvoiceStatus.Issued && x.IssueDate <= asOfDate);
        var invoiceIds = invoices.Select(x => x.Id).ToList();

        var lines = invoiceIds.Count > 0
            ? (await _supplierInvoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId))).ToLookup(x => x.SupplierInvoiceId)
            : Enumerable.Empty<SupplierInvoiceLine>().ToLookup(x => x.SupplierInvoiceId);
        var payments = invoiceIds.Count > 0
            ? (await _vendorPaymentRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId) && x.PaymentDate <= asOfDate)).ToLookup(x => x.SupplierInvoiceId)
            : Enumerable.Empty<VendorPayment>().ToLookup(x => x.SupplierInvoiceId);
        var namesById = (await _vendorRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        var buckets = invoices
            .Select(invoice => new
            {
                invoice.VendorId,
                AmountDue = lines[invoice.Id].Sum(x => x.Subtotal()) - payments[invoice.Id].Sum(x => x.Amount + x.WithholdingTaxAmount),
                DaysOverdue = (asOfDate.Date - invoice.DueDate.Date).Days
            })
            .Where(x => x.AmountDue > 0);

        return BuildReport(asOfDate, buckets.Select(x => (x.VendorId, x.AmountDue, x.DaysOverdue)), namesById);
    }

    private static AgingReportDto BuildReport(DateTime asOfDate, IEnumerable<(Guid PartyId, decimal AmountDue, int DaysOverdue)> items, Dictionary<Guid, string> namesById)
    {
        var rowsByParty = new Dictionary<Guid, AgingRowDto>();

        foreach (var item in items)
        {
            if (!rowsByParty.TryGetValue(item.PartyId, out var row))
            {
                row = new AgingRowDto { PartyId = item.PartyId, PartyName = namesById.GetValueOrDefault(item.PartyId, "Unknown") };
                rowsByParty[item.PartyId] = row;
            }

            if (item.DaysOverdue <= 0)
            {
                row.Current += item.AmountDue;
            }
            else if (item.DaysOverdue <= 30)
            {
                row.Days1To30 += item.AmountDue;
            }
            else if (item.DaysOverdue <= 60)
            {
                row.Days31To60 += item.AmountDue;
            }
            else if (item.DaysOverdue <= 90)
            {
                row.Days61To90 += item.AmountDue;
            }
            else
            {
                row.Over90 += item.AmountDue;
            }

            row.Total += item.AmountDue;
        }

        var rows = rowsByParty.Values.OrderByDescending(x => x.Total).ToList();

        return new AgingReportDto
        {
            AsOfDate = asOfDate,
            Rows = rows,
            TotalCurrent = rows.Sum(x => x.Current),
            TotalDays1To30 = rows.Sum(x => x.Days1To30),
            TotalDays31To60 = rows.Sum(x => x.Days31To60),
            TotalDays61To90 = rows.Sum(x => x.Days61To90),
            TotalOver90 = rows.Sum(x => x.Over90),
            GrandTotal = rows.Sum(x => x.Total)
        };
    }
}
