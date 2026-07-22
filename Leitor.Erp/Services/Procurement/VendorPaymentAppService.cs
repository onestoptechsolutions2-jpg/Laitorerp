using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class VendorPaymentAppService :
    CrudAppService<VendorPayment, VendorPaymentDto, Guid, GetVendorPaymentListInput, CreateUpdateVendorPaymentDto>
{
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IDataFilter _dataFilter;

    public VendorPaymentAppService(
        IRepository<VendorPayment, Guid> repository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _vendorRepository = vendorRepository;
        _taxRateRepository = taxRateRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _dataFilter = dataFilter;
        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<VendorPayment>> CreateFilteredQueryAsync(GetVendorPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.SupplierInvoiceId.HasValue, x => x.SupplierInvoiceId == input.SupplierInvoiceId!.Value);
    }

    protected override async Task<VendorPayment> MapToEntityAsync(CreateUpdateVendorPaymentDto createInput)
    {
        var entity = new VendorPayment(GuidGenerator.Create(), createInput.SupplierInvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);

        var supplierInvoice = await _supplierInvoiceRepository.GetAsync(createInput.SupplierInvoiceId);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            entity.CurrencyCode = supplierInvoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);

        // Withheld at source: the vendor's own WithholdingTaxRateId (if any) applies to the full
        // payment amount - a Kenyan-tax-compliance concept (see Entities/Sales/TaxType.cs),
        // snapshotted here rather than recomputed later, same discipline as every other tax field.
        var vendor = await _vendorRepository.GetAsync(supplierInvoice.VendorId);
        entity.WithholdingTaxAmount = 0;
        if (vendor.WithholdingTaxRateId.HasValue)
        {
            var withholdingRate = await _taxRateRepository.FindAsync(vendor.WithholdingTaxRateId.Value);
            if (withholdingRate != null)
            {
                entity.WithholdingTaxAmount = Math.Round(entity.Amount * withholdingRate.Percent / 100m, 2);
            }
        }

        // Same reasoning as PaymentAppService: a VendorPayment is always a single atomic,
        // known-amount transaction, so it always auto-posts immediately. Without withholding, this
        // is the original single Dr AP / Cr Cash entry. With withholding, the AP liability still
        // clears for the full Amount, but only (Amount - WithholdingTaxAmount) actually leaves
        // Cash - the difference moves to Withholding Tax Payable instead of being paid out, so
        // it's posted as a second entry rather than reshaping PostAsync's fixed two-line shape.
        var cashAmount = entity.Amount - entity.WithholdingTaxAmount;

        await JournalPostingService.PostAsync(
            _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            entity.PaymentDate, JournalPostingService.SourceDocumentTypes.VendorPayment, entity.Id,
            $"Payment sent - Supplier Invoice {supplierInvoice.SupplierInvoiceNumber}",
            SystemAccountRole.AccountsPayable, SystemAccountRole.Cash,
            cashAmount, entity.CurrencyCode, entity.ExchangeRateToBase);

        if (entity.WithholdingTaxAmount > 0)
        {
            await JournalPostingService.PostAsync(
                _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
                entity.PaymentDate, JournalPostingService.SourceDocumentTypes.VendorPayment, entity.Id,
                $"Withholding tax - Supplier Invoice {supplierInvoice.SupplierInvoiceNumber}",
                SystemAccountRole.AccountsPayable, SystemAccountRole.WithholdingTaxPayable,
                entity.WithholdingTaxAmount, entity.CurrencyCode, entity.ExchangeRateToBase);
        }

        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateVendorPaymentDto updateInput, VendorPayment entity)
    {
        CopyToEntity(updateInput, entity);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var supplierInvoice = await _supplierInvoiceRepository.GetAsync(updateInput.SupplierInvoiceId);
            entity.CurrencyCode = supplierInvoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);
    }

    private static void CopyToEntity(CreateUpdateVendorPaymentDto input, VendorPayment entity)
    {
        entity.SupplierInvoiceId = input.SupplierInvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
        entity.CurrencyCode = input.CurrencyCode ?? string.Empty;
    }
}
