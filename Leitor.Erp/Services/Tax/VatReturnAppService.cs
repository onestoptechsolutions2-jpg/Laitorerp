using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Tax;
using Leitor.Erp.Services;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Tax;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// GeneralLedgerReportAppService. Produces the figures a Kenyan VAT return needs; it does not file
// anything - there's no known jurisdiction e-filing API to integrate against, so submission stays
// a manual step for whoever prepares the return.
//
// Both Output VAT and Input VAT are now exact - InvoiceLine/SupplierInvoiceLine.TaxRatePercent are
// each captured per line at add-time (see TaxRateResolver). Input VAT used to approximate off a
// single default rate applied to SupplierInvoiceLine totals, back when Procurement had no per-line
// tax rate at all - see Entities/Procurement/PurchaseOrderLine.cs's own comment for that fix.
[RequiresFeature(ErpFeatures.TaxCompliance)]
public class VatReturnAppService : ApplicationService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;

    public VatReturnAppService(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
    }

    public async Task<VatReturnDto> GetVatReturnAsync(DateTime fromDate, DateTime toDate)
    {
        await CheckPolicyAsync(ErpPermissions.TaxCompliance.Default);

        var outputVat = await ComputeOutputVatAsync(fromDate, toDate);
        var inputVat = await ComputeInputVatAsync(fromDate, toDate);

        return new VatReturnDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            OutputVat = outputVat,
            InputVat = inputVat,
            NetVatPayable = outputVat - inputVat
        };
    }

    private async Task<decimal> ComputeOutputVatAsync(DateTime fromDate, DateTime toDate)
    {
        var invoices = await _invoiceRepository.GetListAsync(
            x => x.Status == InvoiceStatus.Issued && x.IssueDate >= fromDate && x.IssueDate <= toDate);
        if (invoices.Count == 0)
        {
            return 0;
        }

        var invoicesById = invoices.ToDictionary(x => x.Id);
        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var lines = await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));

        return lines.Sum(line =>
        {
            var invoice = invoicesById[line.InvoiceId];
            return line.TaxAmount() * invoice.ExchangeRateToBase;
        });
    }

    private async Task<decimal> ComputeInputVatAsync(DateTime fromDate, DateTime toDate)
    {
        var supplierInvoices = await _supplierInvoiceRepository.GetListAsync(
            x => x.Status == SupplierInvoiceStatus.Issued && x.IssueDate >= fromDate && x.IssueDate <= toDate);
        if (supplierInvoices.Count == 0)
        {
            return 0;
        }

        var invoicesById = supplierInvoices.ToDictionary(x => x.Id);
        var invoiceIds = supplierInvoices.Select(x => x.Id).ToList();
        var lines = await _supplierInvoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId));

        return lines.Sum(line =>
        {
            var supplierInvoice = invoicesById[line.SupplierInvoiceId];
            return line.TaxAmount() * supplierInvoice.ExchangeRateToBase;
        });
    }
}
