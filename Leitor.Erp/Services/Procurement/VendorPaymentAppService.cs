using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Procurement;
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
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IDataFilter _dataFilter;

    public VendorPaymentAppService(
        IRepository<VendorPayment, Guid> repository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
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

        // Same reasoning as PaymentAppService: a VendorPayment is always a single atomic,
        // known-amount transaction, so it always auto-posts Dr Accounts Payable / Cr Cash
        // immediately.
        await JournalPostingService.PostAsync(
            _accountRepository, _journalEntryRepository, _journalEntryLineRepository, GuidGenerator, _dataFilter,
            entity.PaymentDate, JournalPostingService.SourceDocumentTypes.VendorPayment, entity.Id,
            $"Payment sent - Supplier Invoice {supplierInvoice.SupplierInvoiceNumber}",
            SystemAccountRole.AccountsPayable, SystemAccountRole.Cash,
            entity.Amount, entity.CurrencyCode, entity.ExchangeRateToBase);

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
