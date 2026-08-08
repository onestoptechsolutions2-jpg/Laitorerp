using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Sales;

public class InvoiceAppService :
    CrudAppService<Invoice, InvoiceDto, Guid, GetInvoiceListInput, CreateUpdateInvoiceDto>
{
    private readonly IRepository<InvoiceLine, Guid> _lineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public InvoiceAppService(
        IRepository<Invoice, Guid> repository,
        IRepository<InvoiceLine, Guid> lineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _orderRepository = orderRepository;
        _identityUserRepository = identityUserRepository;
        _dataFilter = dataFilter;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Create;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Delete;
    }

    // Lines and payments are independent aggregate roots with no FK relationship configured -
    // same cascade pattern as CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Invoice", id);

        var lines = await _lineRepository.GetListAsync(x => x.InvoiceId == id);
        await _lineRepository.DeleteManyAsync(lines);

        var payments = await _paymentRepository.GetListAsync(x => x.InvoiceId == id);
        await _paymentRepository.DeleteManyAsync(payments);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Invoice>> CreateFilteredQueryAsync(GetInvoiceListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.InvoiceNumber.Contains(input.Filter!));
    }

    public override async Task<InvoiceDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<InvoiceDto>> GetListAsync(GetInvoiceListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<InvoiceDto> invoices)
    {
        var customerIds = invoices.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var namesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var invoiceIds = invoices.Select(x => x.Id).ToList();
        var allLines = await _lineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
        var linesByInvoiceId = allLines.ToLookup(x => x.InvoiceId);

        var allPayments = await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId));
        var paymentsByInvoiceId = allPayments.ToLookup(x => x.InvoiceId);

        var postedInvoiceIds = (await _journalEntryRepository.GetListAsync(
            x => x.SourceDocumentType == JournalPostingService.SourceDocumentTypes.Invoice && x.SourceDocumentId != null && invoiceIds.Contains(x.SourceDocumentId!.Value)))
            .Select(x => x.SourceDocumentId!.Value)
            .ToHashSet();

        var now = Clock.Now;

        var salespersonIds = invoices
            .Where(x => x.SalespersonUserId.HasValue)
            .Select(x => x.SalespersonUserId!.Value)
            .Distinct()
            .ToList();
        var salespersonNamesById = salespersonIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => salespersonIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var invoice in invoices)
        {
            if (namesById.TryGetValue(invoice.CustomerId, out var customerName))
            {
                invoice.CustomerName = customerName;
            }

            var lines = linesByInvoiceId[invoice.Id].ToList();
            invoice.Subtotal = lines.Sum(x => x.Subtotal());
            invoice.TaxAmount = lines.Sum(x => x.TaxAmount());
            invoice.Total = invoice.Subtotal + invoice.TaxAmount;
            invoice.AmountPaid = paymentsByInvoiceId[invoice.Id].Sum(x => x.Amount);
            invoice.IsPostedToLedger = postedInvoiceIds.Contains(invoice.Id);

            invoice.PaymentStatus = ComputePaymentStatus(invoice, now);

            if (invoice.SalespersonUserId.HasValue && salespersonNamesById.TryGetValue(invoice.SalespersonUserId.Value, out var salespersonName))
            {
                invoice.SalespersonName = salespersonName;
            }
        }
    }

    // The standalone "New Invoice" flow (unlike Order->Invoice conversion) creates the header
    // before any lines exist, so there's no single atomic moment to auto-post from - staff click
    // this once they're done adding lines. Guarded so an invoice can only be posted once, and only
    // once it's actually Issued with a non-zero total.
    public async Task PostToLedgerAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Edit);

        var alreadyPosted = await JournalPostingService.IsAlreadyPostedAsync(_journalEntryRepository, JournalPostingService.SourceDocumentTypes.Invoice, id);
        if (alreadyPosted)
        {
            throw new UserFriendlyException("This invoice has already been posted to the ledger.");
        }

        var invoice = await Repository.GetAsync(id);
        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new UserFriendlyException("Only an issued invoice can be posted to the ledger.");
        }

        var lines = await _lineRepository.GetListAsync(x => x.InvoiceId == id);
        var total = lines.Sum(x => x.Total());
        if (total <= 0)
        {
            throw new UserFriendlyException("Add at least one line before posting this invoice to the ledger.");
        }

        await JournalPostingService.PostAsync(
            _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            invoice.IssueDate, JournalPostingService.SourceDocumentTypes.Invoice, invoice.Id,
            $"Invoice {invoice.InvoiceNumber}",
            SystemAccountRole.AccountsReceivable, SystemAccountRole.Revenue,
            total, invoice.CurrencyCode, invoice.ExchangeRateToBase);
    }

    // Computed exactly like Manager.io: never manually set, always derived from payments applied.
    private static InvoicePaymentStatus ComputePaymentStatus(InvoiceDto invoice, DateTime now)
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

    protected override async Task<Invoice> MapToEntityAsync(CreateUpdateInvoiceDto createInput)
    {
        var invoiceNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "INV-");

        var entity = new Invoice(GuidGenerator.Create(), createInput.CustomerId, invoiceNumber);
        CopyToEntity(createInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
        entity.SalespersonUserId = await ResolveSalespersonUserIdAsync(createInput.SalespersonUserId, createInput.OrderId);
        return entity;
    }

    // Purely attributive (commission/reporting) - see Invoice.SalespersonUserId's own comment.
    // Respects an explicit value if one is ever passed in; otherwise carries forward the source
    // Order's SalespersonUserId when this Invoice was created via the standalone Create page's
    // optional OrderId field (the primary paths, OrderAppService.ConvertToInvoiceAsync/
    // CreateInvoiceForMilestoneAsync, already set it directly), else falls back to whoever is
    // creating the Invoice directly.
    private async Task<Guid?> ResolveSalespersonUserIdAsync(Guid? explicitValue, Guid? orderId)
    {
        if (explicitValue.HasValue)
        {
            return explicitValue;
        }

        if (orderId.HasValue)
        {
            var order = await _orderRepository.FindAsync(orderId.Value);
            if (order?.SalespersonUserId != null)
            {
                return order.SalespersonUserId;
            }
        }

        return CurrentUser.Id;
    }

    protected override async Task MapToEntityAsync(CreateUpdateInvoiceDto updateInput, Invoice entity)
    {
        CopyToEntity(updateInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
    }

    private static void CopyToEntity(CreateUpdateInvoiceDto input, Invoice entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.OrderId = input.OrderId;
        entity.Status = input.Status;
        entity.IssueDate = input.IssueDate;
        entity.DueDate = input.DueDate;
        entity.Notes = input.Notes;
        entity.PaymentTerms = input.PaymentTerms;
        entity.CurrencyCode = input.CurrencyCode;
    }
}
