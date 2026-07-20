using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class SupplierInvoiceAppService :
    CrudAppService<SupplierInvoice, SupplierInvoiceDto, Guid, GetSupplierInvoiceListInput, CreateUpdateSupplierInvoiceDto>
{
    private readonly IRepository<SupplierInvoiceLine, Guid> _lineRepository;
    private readonly IRepository<VendorPayment, Guid> _paymentRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public SupplierInvoiceAppService(
        IRepository<SupplierInvoice, Guid> repository,
        IRepository<SupplierInvoiceLine, Guid> lineRepository,
        IRepository<VendorPayment, Guid> paymentRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _paymentRepository = paymentRepository;
        _vendorRepository = vendorRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _dataFilter = dataFilter;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Create;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Delete;
    }

    // Lines and payments are independent aggregate roots with no FK relationship configured -
    // same cascade pattern as InvoiceAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "SupplierInvoice", id);

        var lines = await _lineRepository.GetListAsync(x => x.SupplierInvoiceId == id);
        await _lineRepository.DeleteManyAsync(lines);

        var payments = await _paymentRepository.GetListAsync(x => x.SupplierInvoiceId == id);
        await _paymentRepository.DeleteManyAsync(payments);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<SupplierInvoice>> CreateFilteredQueryAsync(GetSupplierInvoiceListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.VendorId.HasValue, x => x.VendorId == input.VendorId!.Value)
            .WhereIf(input.PurchaseOrderId.HasValue, x => x.PurchaseOrderId == input.PurchaseOrderId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.SupplierInvoiceNumber.Contains(input.Filter!));
    }

    public override async Task<SupplierInvoiceDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<SupplierInvoiceDto>> GetListAsync(GetSupplierInvoiceListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<SupplierInvoiceDto> invoices)
    {
        var vendorIds = invoices.Select(x => x.VendorId).Distinct().ToList();
        var vendors = await _vendorRepository.GetListAsync(x => vendorIds.Contains(x.Id));
        var vendorNamesById = vendors.ToDictionary(x => x.Id, x => x.Name);

        var purchaseOrderIds = invoices.Select(x => x.PurchaseOrderId).Distinct().ToList();
        var purchaseOrders = await _purchaseOrderRepository.GetListAsync(x => purchaseOrderIds.Contains(x.Id));
        var poNumbersById = purchaseOrders.ToDictionary(x => x.Id, x => x.PONumber);

        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var allLines = await _lineRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId));
        var linesByInvoiceId = allLines.ToLookup(x => x.SupplierInvoiceId);

        var allPayments = await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.SupplierInvoiceId));
        var paymentsByInvoiceId = allPayments.ToLookup(x => x.SupplierInvoiceId);

        var postedInvoiceIds = (await _journalEntryRepository.GetListAsync(
            x => x.SourceDocumentType == JournalPostingService.SourceDocumentTypes.SupplierInvoice && x.SourceDocumentId != null && invoiceIds.Contains(x.SourceDocumentId!.Value)))
            .Select(x => x.SourceDocumentId!.Value)
            .ToHashSet();

        var now = Clock.Now;

        foreach (var invoice in invoices)
        {
            if (vendorNamesById.TryGetValue(invoice.VendorId, out var vendorName))
            {
                invoice.VendorName = vendorName;
            }

            if (poNumbersById.TryGetValue(invoice.PurchaseOrderId, out var poNumber))
            {
                invoice.PONumber = poNumber;
            }

            invoice.Total = linesByInvoiceId[invoice.Id]
                .Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
            invoice.AmountPaid = paymentsByInvoiceId[invoice.Id].Sum(x => x.Amount);
            invoice.IsPostedToLedger = postedInvoiceIds.Contains(invoice.Id);

            invoice.PaymentStatus = ComputePaymentStatus(invoice, now);
        }
    }

    // Same reasoning as InvoiceAppService.PostToLedgerAsync - the standalone Create page inserts
    // this header and its PO-derived lines in separate calls (from the page, not atomically in
    // one AppService method), so there's no single moment to auto-post from. The Create page
    // calls this once its line-insertion loop finishes; it's also exposed as a button on Detail
    // in case that call ever fails/is skipped.
    public async Task PostToLedgerAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Procurement.Edit);

        var alreadyPosted = await JournalPostingService.IsAlreadyPostedAsync(_journalEntryRepository, JournalPostingService.SourceDocumentTypes.SupplierInvoice, id);
        if (alreadyPosted)
        {
            throw new UserFriendlyException("This supplier invoice has already been posted to the ledger.");
        }

        var invoice = await Repository.GetAsync(id);
        if (invoice.Status != SupplierInvoiceStatus.Issued)
        {
            throw new UserFriendlyException("Only an issued supplier invoice can be posted to the ledger.");
        }

        var lines = await _lineRepository.GetListAsync(x => x.SupplierInvoiceId == id);
        var total = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
        if (total <= 0)
        {
            throw new UserFriendlyException("Add at least one line before posting this supplier invoice to the ledger.");
        }

        await JournalPostingService.PostAsync(
            _accountRepository, _journalEntryRepository, _journalEntryLineRepository, GuidGenerator, _dataFilter,
            invoice.IssueDate, JournalPostingService.SourceDocumentTypes.SupplierInvoice, invoice.Id,
            $"Supplier Invoice {invoice.SupplierInvoiceNumber}",
            SystemAccountRole.Expense, SystemAccountRole.AccountsPayable,
            total, invoice.CurrencyCode, invoice.ExchangeRateToBase);
    }

    // Same rule InvoiceAppService.ComputePaymentStatus already uses, on the payable side.
    private static InvoicePaymentStatus ComputePaymentStatus(SupplierInvoiceDto invoice, DateTime now)
    {
        if (invoice.AmountPaid <= 0)
        {
            return invoice.DueDate < now ? InvoicePaymentStatus.Overdue : InvoicePaymentStatus.Unpaid;
        }

        if (invoice.AmountPaid < invoice.Total)
        {
            return InvoicePaymentStatus.PartiallyPaid;
        }

        return invoice.AmountPaid > invoice.Total ? InvoicePaymentStatus.Overpaid : InvoicePaymentStatus.PaidInFull;
    }

    protected override async Task<SupplierInvoice> MapToEntityAsync(CreateUpdateSupplierInvoiceDto createInput)
    {
        var entity = new SupplierInvoice(GuidGenerator.Create(), createInput.PurchaseOrderId, createInput.VendorId, createInput.SupplierInvoiceNumber);
        CopyToEntity(createInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateSupplierInvoiceDto updateInput, SupplierInvoice entity)
    {
        CopyToEntity(updateInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
    }

    private static void CopyToEntity(CreateUpdateSupplierInvoiceDto input, SupplierInvoice entity)
    {
        entity.PurchaseOrderId = input.PurchaseOrderId;
        entity.VendorId = input.VendorId;
        entity.SupplierInvoiceNumber = input.SupplierInvoiceNumber;
        entity.Status = input.Status;
        entity.IssueDate = input.IssueDate;
        entity.DueDate = input.DueDate;
        entity.Notes = input.Notes;
        entity.CurrencyCode = input.CurrencyCode;
    }
}
