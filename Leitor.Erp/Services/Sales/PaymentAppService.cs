using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Partners;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PaymentAppService :
    CrudAppService<Payment, PaymentDto, Guid, GetPaymentListInput, CreateUpdatePaymentDto>
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IRepository<Commission, Guid> _commissionRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IDataFilter _dataFilter;

    public PaymentAppService(
        IRepository<Payment, Guid> repository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IRepository<Commission, Guid> commissionRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _commissionRepository = commissionRepository;
        _orderRepository = orderRepository;
        _quoteRepository = quoteRepository;
        _proposalRepository = proposalRepository;
        _stageEventRepository = stageEventRepository;
        _dataFilter = dataFilter;
        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<Payment>> CreateFilteredQueryAsync(GetPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.InvoiceId.HasValue, x => x.InvoiceId == input.InvoiceId!.Value);
    }

    protected override async Task<Payment> MapToEntityAsync(CreateUpdatePaymentDto createInput)
    {
        var entity = new Payment(GuidGenerator.Create(), createInput.InvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);

        var invoice = await _invoiceRepository.GetAsync(createInput.InvoiceId);

        // CurrencyCode is optional on the DTO - defaults from the parent Invoice when the caller
        // doesn't specify one (the common case: paid in the same currency it was billed in).
        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            entity.CurrencyCode = invoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate, throwIfNotFound: false);

        // Every Payment is a single atomic, known-amount transaction (unlike Invoice/SupplierInvoice,
        // whose lines are added afterward) - so it always auto-posts Dr Cash / Cr Accounts
        // Receivable immediately, no separate "Post to Ledger" step needed.
        await JournalPostingService.PostAsync(
            _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            entity.PaymentDate, JournalPostingService.SourceDocumentTypes.Payment, entity.Id,
            $"Payment received - Invoice {invoice.InvoiceNumber}",
            SystemAccountRole.Cash, SystemAccountRole.AccountsReceivable,
            entity.Amount, entity.CurrencyCode, entity.ExchangeRateToBase);

        // Flips any Pending, OnClientPayment-triggered Commission tied to this Invoice to Payable
        // (TC-025) - only once the invoice is actually paid in full, not on the first partial
        // payment, so a partner/agent can never become payable for more than the client has
        // actually paid. Same Total()/AmountPaid computation InvoiceAppService.ComputePaymentStatus
        // uses, duplicated here rather than shared since that method is DTO-shaped and private.
        var invoiceLines = await _invoiceLineRepository.GetListAsync(x => x.InvoiceId == entity.InvoiceId);
        var invoiceTotal = invoiceLines.Sum(x => x.Total());
        var existingPayments = await Repository.GetListAsync(x => x.InvoiceId == entity.InvoiceId && x.Id != entity.Id);
        var amountPaid = existingPayments.Sum(x => x.Amount) + entity.Amount;

        if (amountPaid >= invoiceTotal)
        {
            await CommissionAutoPayableService.MarkPayableForInvoiceAsync(
                _commissionRepository, _invoiceRepository, _orderRepository, _quoteRepository, _proposalRepository,
                _stageEventRepository, GuidGenerator, CurrentUser, Clock, entity.InvoiceId);
        }

        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdatePaymentDto updateInput, Payment entity)
    {
        CopyToEntity(updateInput, entity);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var invoice = await _invoiceRepository.GetAsync(updateInput.InvoiceId);
            entity.CurrencyCode = invoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate, throwIfNotFound: false);
    }

    private static void CopyToEntity(CreateUpdatePaymentDto input, Payment entity)
    {
        entity.InvoiceId = input.InvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
        entity.CurrencyCode = input.CurrencyCode ?? string.Empty;
    }
}
