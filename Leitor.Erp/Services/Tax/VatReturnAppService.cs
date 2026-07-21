using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Tax;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Tax;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// GeneralLedgerReportAppService. Produces the figures a Kenyan VAT return needs; it does not file
// anything - there's no known jurisdiction e-filing API to integrate against, so submission stays
// a manual step for whoever prepares the return.
//
// Output VAT is exact - InvoiceLine.TaxRatePercent is already captured per line at issue time.
// Input VAT is an approximation: Procurement (PurchaseOrderLine/SupplierInvoiceLine) has never
// captured a per-line tax rate the way Sales does, so this applies the single default VAT rate to
// every SupplierInvoiceLine's taxable base rather than a real per-line rate. Flagged here rather
// than silently treated as exact - a deliberate v1 scope cut to avoid retrofitting tax fields onto
// every Procurement line entity as a prerequisite for this report.
[RequiresFeature(ErpFeatures.TaxCompliance)]
public class VatReturnAppService : ApplicationService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;

    public VatReturnAppService(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<TaxRate, Guid> taxRateRepository)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _taxRateRepository = taxRateRepository;
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
            var taxableBase = line.UnitPrice * line.Quantity * (1 - line.DiscountPercent / 100m);
            return taxableBase * line.TaxRatePercent / 100m * invoice.ExchangeRateToBase;
        });
    }

    private async Task<decimal> ComputeInputVatAsync(DateTime fromDate, DateTime toDate)
    {
        var defaultVatRate = (await _taxRateRepository.GetListAsync(x => x.IsDefault && x.TaxType == TaxType.Vat)).FirstOrDefault();
        if (defaultVatRate == null || defaultVatRate.Percent == 0)
        {
            return 0;
        }

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
            var taxableBase = line.UnitPrice * line.Quantity * (1 - line.DiscountPercent / 100m);
            return taxableBase * defaultVatRate.Percent / 100m * supplierInvoice.ExchangeRateToBase;
        });
    }
}
