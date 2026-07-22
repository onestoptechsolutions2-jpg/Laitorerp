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

// Chronological Invoice(charge)/Payment(credit) history with a running balance - same
// face-currency-only convention as AgingReportAppService (no base-currency conversion).
public class StatementAppService : ApplicationService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<VendorPayment, Guid> _vendorPaymentRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public StatementAppService(
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

    public async Task<StatementDto> GetCustomerStatementAsync(Guid customerId, DateTime fromDate, DateTime toDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var customer = await _customerRepository.GetAsync(customerId);

        var allInvoices = await _invoiceRepository.GetListAsync(x => x.CustomerId == customerId && x.Status == InvoiceStatus.Issued);
        var invoiceIds = allInvoices.Select(x => x.Id).ToList();
        var linesByInvoiceId = invoiceIds.Count > 0
            ? (await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId))).ToLookup(x => x.InvoiceId)
            : Enumerable.Empty<InvoiceLine>().ToLookup(x => x.InvoiceId);
        var allPayments = invoiceIds.Count > 0
            ? await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId))
            : new List<Payment>();

        var charges = allInvoices.Select(x => (Date: x.IssueDate, Description: $"Invoice {x.InvoiceNumber}", Amount: linesByInvoiceId[x.Id].Sum(l => l.Total())));
        var credits = allPayments.Select(x => (Date: x.PaymentDate, Description: "Payment received", Amount: x.Amount));

        return BuildStatement(customerId, customer.Name, fromDate, toDate, charges, credits);
    }

    public async Task<StatementDto> GetVendorStatementAsync(Guid vendorId, DateTime fromDate, DateTime toDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var vendor = await _vendorRepository.GetAsync(vendorId);

        var allInvoices = await _supplierInvoiceRepository.GetListAsync(x => x.VendorId == vendorId && x.Status == SupplierInvoiceStatus.Issued);
        var invoiceIds = allInvoices.Select(x => x.Id).ToList();
        var linesByInvoiceId = invoiceIds.Count > 0
            ? (await _supplierInvoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId))).ToLookup(x => x.SupplierInvoiceId)
            : Enumerable.Empty<SupplierInvoiceLine>().ToLookup(x => x.SupplierInvoiceId);
        var allPayments = invoiceIds.Count > 0
            ? await _vendorPaymentRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId))
            : new List<VendorPayment>();

        var charges = allInvoices.Select(x => (Date: x.IssueDate, Description: $"Supplier Invoice {x.SupplierInvoiceNumber}", Amount: linesByInvoiceId[x.Id].Sum(l => l.Subtotal())));
        var credits = allPayments.Select(x => (Date: x.PaymentDate, Description: "Payment made", Amount: x.Amount + x.WithholdingTaxAmount));

        return BuildStatement(vendorId, vendor.Name, fromDate, toDate, charges, credits);
    }

    private static StatementDto BuildStatement(
        Guid partyId, string partyName, DateTime fromDate, DateTime toDate,
        IEnumerable<(DateTime Date, string Description, decimal Amount)> charges,
        IEnumerable<(DateTime Date, string Description, decimal Amount)> credits)
    {
        var openingBalance =
            charges.Where(x => x.Date < fromDate).Sum(x => x.Amount) -
            credits.Where(x => x.Date < fromDate).Sum(x => x.Amount);

        var periodEntries = charges
            .Where(x => x.Date >= fromDate && x.Date <= toDate)
            .Select(x => new StatementLineDto { Date = x.Date, Description = x.Description, Charge = x.Amount })
            .Concat(credits
                .Where(x => x.Date >= fromDate && x.Date <= toDate)
                .Select(x => new StatementLineDto { Date = x.Date, Description = x.Description, Credit = x.Amount }))
            .OrderBy(x => x.Date)
            .ToList();

        var runningBalance = openingBalance;
        foreach (var line in periodEntries)
        {
            runningBalance += line.Charge - line.Credit;
            line.RunningBalance = runningBalance;
        }

        return new StatementDto
        {
            PartyId = partyId,
            PartyName = partyName,
            FromDate = fromDate,
            ToDate = toDate,
            OpeningBalance = openingBalance,
            ClosingBalance = runningBalance,
            Lines = periodEntries
        };
    }
}
