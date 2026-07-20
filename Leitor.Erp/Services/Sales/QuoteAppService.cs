using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class QuoteAppService :
    CrudAppService<Quote, QuoteDto, Guid, GetQuoteListInput, CreateUpdateQuoteDto>
{
    private readonly IRepository<QuoteLine, Guid> _lineRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IDataFilter _dataFilter;

    public QuoteAppService(
        IRepository<Quote, Guid> repository,
        IRepository<QuoteLine, Guid> lineRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _proposalRepository = proposalRepository;
        _stageEventRepository = stageEventRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Create;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Delete;
    }

    // QuoteLines are an independent aggregate root (see QuoteLine.cs) with no FK relationship
    // configured, so deleting a quote doesn't cascade automatically - same pattern as
    // CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        var lines = await _lineRepository.GetListAsync(x => x.QuoteId == id);
        await _lineRepository.DeleteManyAsync(lines);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Quote>> CreateFilteredQueryAsync(GetQuoteListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Title.Contains(input.Filter!) || x.QuoteNumber.Contains(input.Filter!));
    }

    public override async Task<QuoteDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<QuoteDto>> GetListAsync(GetQuoteListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<QuoteDto> quotes)
    {
        var customerIds = quotes.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var namesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var quoteIds = quotes.Select(x => x.Id).ToList();
        var allLines = await _lineRepository.GetListAsync(x => quoteIds.Contains(x.QuoteId));
        var linesByQuoteId = allLines.ToLookup(x => x.QuoteId);

        var proposalIds = quotes
            .Where(x => x.ProposalId.HasValue)
            .Select(x => x.ProposalId!.Value)
            .Distinct()
            .ToList();
        var proposalNumbersById = proposalIds.Count > 0
            ? (await _proposalRepository.GetListAsync(x => proposalIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.ProposalNumber)
            : new Dictionary<Guid, string>();

        foreach (var quote in quotes)
        {
            if (namesById.TryGetValue(quote.CustomerId, out var customerName))
            {
                quote.CustomerName = customerName;
            }

            var lines = linesByQuoteId[quote.Id].ToList();
            quote.Subtotal = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
            quote.TaxAmount = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m) * x.TaxRatePercent / 100m);
            quote.Total = quote.Subtotal + quote.TaxAmount;

            if (quote.ProposalId.HasValue && proposalNumbersById.TryGetValue(quote.ProposalId.Value, out var proposalNumber))
            {
                quote.ProposalNumber = proposalNumber;
            }
        }
    }

    // CreateUpdateQuoteDto -> Quote is mapped manually rather than via Mapperly - same reason as
    // every other entity in this app (protected Id setter + constructor args Mapperly can't
    // resolve from the DTO).
    protected override async Task<Quote> MapToEntityAsync(CreateUpdateQuoteDto createInput)
    {
        // Mirrors the same check ConvertToQuoteAsync makes - without this, the standalone New
        // Quote page (which also accepts an optional ProposalId) could silently create a second
        // Quote against a Proposal that's already been converted.
        if (createInput.ProposalId.HasValue &&
            (await Repository.GetListAsync(x => x.ProposalId == createInput.ProposalId.Value)).Any())
        {
            throw new UserFriendlyException("This proposal has already been converted to a quote.");
        }

        var quoteNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "Q-");

        var entity = new Quote(GuidGenerator.Create(), createInput.CustomerId, quoteNumber, createInput.Title);
        CopyToEntity(createInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateQuoteDto updateInput, Quote entity)
    {
        // Once a Quote leaves Draft it's locked - editing requires an explicit unlock first (see
        // UnlockForRevisionAsync). Single-use: consumed the moment this edit is saved.
        if (entity.IsLocked && entity.UnlockedByUserId == null)
        {
            throw new UserFriendlyException("This quote is locked because it's no longer a draft. Unlock it for revision before making changes.");
        }

        var wasUnlocked = entity.UnlockedByUserId != null;

        CopyToEntity(updateInput, entity);
        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.IssueDate);
        entity.Version++;

        if (wasUnlocked)
        {
            entity.UnlockedByUserId = null;
            entity.UnlockedAt = null;
            entity.UnlockReason = null;
        }
    }

    // Only a holder of Sales.Unlock (Ops Manager) can unlock an approved Quote for revision.
    public async Task UnlockForRevisionAsync(Guid id, string reason)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Unlock);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("A reason is required to unlock this quote for revision.");
        }

        var entity = await Repository.GetAsync(id);
        entity.UnlockedByUserId = CurrentUser.Id;
        entity.UnlockedAt = Clock.Now;
        entity.UnlockReason = reason;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Quote", entity.Id, WorkflowStage.Unlocked, notes: reason);
    }

    private static void CopyToEntity(CreateUpdateQuoteDto input, Quote entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.Title = input.Title;
        entity.Status = input.Status;
        entity.IssueDate = input.IssueDate;
        entity.ExpiryDate = input.ExpiryDate;
        entity.Notes = input.Notes;
        entity.ProposalId = input.ProposalId;
        entity.CurrencyCode = input.CurrencyCode;
    }

    // The concrete mechanism behind "quote becomes an order" - carries line items and pricing
    // forward instead of the user re-entering them, then marks the quote Accepted (conversion IS
    // the acceptance action, same pattern as ProposalAppService.ConvertToQuoteAsync). Blocked:
    // converting a Quote the customer already rejected or that expired, and converting the same
    // Quote twice (one Quote -> one Order).
    public async Task<OrderDto> ConvertToOrderAsync(Guid quoteId)
    {
        await CheckCreatePolicyAsync();

        var quote = await Repository.GetAsync(quoteId);

        if (quote.Status is QuoteStatus.Rejected or QuoteStatus.Expired)
        {
            throw new UserFriendlyException("This quote was rejected or has expired and can't be converted to an order.");
        }

        var alreadyConverted = (await _orderRepository.GetListAsync(x => x.QuoteId == quote.Id)).Any();
        if (alreadyConverted)
        {
            throw new UserFriendlyException("This quote has already been converted to an order.");
        }

        var quoteLines = await _lineRepository.GetListAsync(x => x.QuoteId == quoteId);

        var orderNumber = await DocumentNumbering.NextAsync(_orderRepository, _dataFilter, "SO-");
        var orderDate = Clock.Now;

        var order = new Order(GuidGenerator.Create(), quote.CustomerId, orderNumber)
        {
            QuoteId = quote.Id,
            OrderDate = orderDate,
            Notes = quote.Notes,
            CurrencyCode = quote.CurrencyCode,
            // Re-resolved at the Order's own creation date rather than copying the Quote's
            // snapshot - a Quote issued in January converting to an Order in March should reflect
            // March's rate, not January's.
            ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, quote.CurrencyCode, orderDate)
        };
        await _orderRepository.InsertAsync(order, autoSave: true);

        foreach (var quoteLine in quoteLines)
        {
            var orderLine = new OrderLine(GuidGenerator.Create(), order.Id, quoteLine.Description, quoteLine.UnitPrice)
            {
                ProductId = quoteLine.ProductId,
                Quantity = quoteLine.Quantity,
                DiscountPercent = quoteLine.DiscountPercent,
                Cost = quoteLine.Cost,
                TaxRateId = quoteLine.TaxRateId,
                TaxRatePercent = quoteLine.TaxRatePercent
            };
            await _orderLineRepository.InsertAsync(orderLine, autoSave: true);
        }

        quote.Status = QuoteStatus.Accepted;
        await Repository.UpdateAsync(quote, autoSave: true);

        return ObjectMapper.Map<Order, OrderDto>(order);
    }
}
