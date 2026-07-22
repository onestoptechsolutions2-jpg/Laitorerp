using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Period-end unrealized FX gain/loss on open foreign-currency AR/AP balances - a manual "Post
// Revaluation Entry" trigger (like DepreciationAppService), not automatic. Nets every open
// document's exposure into at most 3 journal lines (AR adjustment, AP adjustment, FX gain/loss)
// via JournalPostingService.PostMultiLineAsync rather than posting one line per document.
public class CurrencyRevaluationAppService : ApplicationService
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<VendorPayment, Guid> _vendorPaymentRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IDataFilter _dataFilter;

    public CurrencyRevaluationAppService(
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<VendorPayment, Guid> vendorPaymentRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IDataFilter dataFilter)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _vendorPaymentRepository = vendorPaymentRepository;
        _vendorRepository = vendorRepository;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _dataFilter = dataFilter;
    }

    public async Task<CurrencyRevaluationPreviewDto> GetPreviewAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);
        return await BuildPreviewAsync(asOfDate);
    }

    public async Task PostRevaluationAsync(DateTime asOfDate)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Create);

        var preview = await BuildPreviewAsync(asOfDate);
        if (preview.Lines.Count == 0)
        {
            throw new UserFriendlyException("There are no open foreign-currency documents to revalue.");
        }

        var arAccount = await ResolveSystemAccountAsync(SystemAccountRole.AccountsReceivable);
        var apAccount = await ResolveSystemAccountAsync(SystemAccountRole.AccountsPayable);
        var fxAccount = await ResolveSystemAccountAsync(SystemAccountRole.UnrealizedFxGainLoss);

        var entryLines = new List<JournalPostingService.MultiLineEntry>();

        if (preview.NetArChangeBase > 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(arAccount.Id, preview.NetArChangeBase, 0, preview.BaseCurrencyCode, 1m));
        }
        else if (preview.NetArChangeBase < 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(arAccount.Id, 0, -preview.NetArChangeBase, preview.BaseCurrencyCode, 1m));
        }

        if (preview.NetApChangeBase > 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(apAccount.Id, 0, preview.NetApChangeBase, preview.BaseCurrencyCode, 1m));
        }
        else if (preview.NetApChangeBase < 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(apAccount.Id, -preview.NetApChangeBase, 0, preview.BaseCurrencyCode, 1m));
        }

        if (preview.NetGainLoss > 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(fxAccount.Id, 0, preview.NetGainLoss, preview.BaseCurrencyCode, 1m));
        }
        else if (preview.NetGainLoss < 0)
        {
            entryLines.Add(new JournalPostingService.MultiLineEntry(fxAccount.Id, -preview.NetGainLoss, 0, preview.BaseCurrencyCode, 1m));
        }

        if (entryLines.Count < 2)
        {
            throw new UserFriendlyException("No net revaluation adjustment is needed as of this date.");
        }

        await JournalPostingService.PostMultiLineAsync(
            _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            asOfDate, "CurrencyRevaluation", GuidGenerator.Create(),
            $"Currency revaluation as of {asOfDate:d}", entryLines);
    }

    private async Task<CurrencyRevaluationPreviewDto> BuildPreviewAsync(DateTime asOfDate)
    {
        var baseCurrencyCode = (await _currencyRepository.GetListAsync(x => x.IsBaseCurrency)).FirstOrDefault()?.Code
            ?? throw new UserFriendlyException("No base currency is configured yet - set one on the Currencies page first.");

        var lines = new List<RevaluationLineDto>();

        var invoices = (await _invoiceRepository.GetListAsync(x => x.Status == InvoiceStatus.Issued && x.CurrencyCode != baseCurrencyCode));
        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var invoiceLines = invoiceIds.Count > 0
            ? (await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId))).ToLookup(x => x.InvoiceId)
            : Enumerable.Empty<InvoiceLine>().ToLookup(x => x.InvoiceId);
        var payments = invoiceIds.Count > 0
            ? (await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId))).ToLookup(x => x.InvoiceId)
            : Enumerable.Empty<Payment>().ToLookup(x => x.InvoiceId);
        var customerNamesById = (await _customerRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        foreach (var invoice in invoices)
        {
            var amountDue = invoiceLines[invoice.Id].Sum(x => x.Total()) - payments[invoice.Id].Sum(x => x.Amount);
            if (amountDue <= 0)
            {
                continue;
            }

            var currentRate = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, invoice.CurrencyCode, asOfDate);
            lines.Add(new RevaluationLineDto
            {
                DocumentType = "Invoice",
                DocumentNumber = invoice.InvoiceNumber,
                PartyName = customerNamesById.GetValueOrDefault(invoice.CustomerId, "Unknown"),
                CurrencyCode = invoice.CurrencyCode,
                AmountDueForeign = amountDue,
                OriginalRate = invoice.ExchangeRateToBase,
                CurrentRate = currentRate,
                GainLossBase = (currentRate - invoice.ExchangeRateToBase) * amountDue
            });
        }

        var supplierInvoices = await _supplierInvoiceRepository.GetListAsync(x => x.Status == SupplierInvoiceStatus.Issued && x.CurrencyCode != baseCurrencyCode);
        var supplierInvoiceIds = supplierInvoices.Select(x => x.Id).ToList();
        var supplierInvoiceLines = supplierInvoiceIds.Count > 0
            ? (await _supplierInvoiceLineRepository.GetListAsync(x => supplierInvoiceIds.Contains(x.SupplierInvoiceId))).ToLookup(x => x.SupplierInvoiceId)
            : Enumerable.Empty<SupplierInvoiceLine>().ToLookup(x => x.SupplierInvoiceId);
        var vendorPayments = supplierInvoiceIds.Count > 0
            ? (await _vendorPaymentRepository.GetListAsync(x => supplierInvoiceIds.Contains(x.SupplierInvoiceId))).ToLookup(x => x.SupplierInvoiceId)
            : Enumerable.Empty<VendorPayment>().ToLookup(x => x.SupplierInvoiceId);
        var vendorNamesById = (await _vendorRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        foreach (var supplierInvoice in supplierInvoices)
        {
            var amountDue = supplierInvoiceLines[supplierInvoice.Id].Sum(x => x.Subtotal())
                - vendorPayments[supplierInvoice.Id].Sum(x => x.Amount + x.WithholdingTaxAmount);
            if (amountDue <= 0)
            {
                continue;
            }

            var currentRate = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, supplierInvoice.CurrencyCode, asOfDate);
            lines.Add(new RevaluationLineDto
            {
                DocumentType = "SupplierInvoice",
                DocumentNumber = supplierInvoice.SupplierInvoiceNumber,
                PartyName = vendorNamesById.GetValueOrDefault(supplierInvoice.VendorId, "Unknown"),
                CurrencyCode = supplierInvoice.CurrencyCode,
                AmountDueForeign = amountDue,
                OriginalRate = supplierInvoice.ExchangeRateToBase,
                CurrentRate = currentRate,
                GainLossBase = (currentRate - supplierInvoice.ExchangeRateToBase) * amountDue
            });
        }

        var netArChange = lines.Where(x => x.DocumentType == "Invoice").Sum(x => x.GainLossBase);
        var netApChange = lines.Where(x => x.DocumentType == "SupplierInvoice").Sum(x => x.GainLossBase);

        return new CurrencyRevaluationPreviewDto
        {
            AsOfDate = asOfDate,
            BaseCurrencyCode = baseCurrencyCode,
            Lines = lines.OrderBy(x => x.DocumentType).ThenBy(x => x.DocumentNumber).ToList(),
            NetArChangeBase = netArChange,
            NetApChangeBase = netApChange,
            NetGainLoss = netArChange - netApChange
        };
    }

    private async Task<Account> ResolveSystemAccountAsync(SystemAccountRole role)
    {
        var account = (await _accountRepository.GetListAsync(x => x.SystemRole == role)).FirstOrDefault();
        if (account == null)
        {
            throw new UserFriendlyException(
                $"No account is configured with the \"{role}\" role yet - set one on the Chart of Accounts page first.");
        }

        return account;
    }
}
