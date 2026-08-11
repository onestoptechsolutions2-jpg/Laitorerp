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
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

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
    private readonly IRepository<PriceListItem, Guid> _priceListItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
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
        IRepository<PriceListItem, Guid> priceListItemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
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
        _priceListItemRepository = priceListItemRepository;
        _productRepository = productRepository;
        _opportunityRepository = opportunityRepository;
        _identityUserRepository = identityUserRepository;
        _dataFilter = dataFilter;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Create;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Delete;
    }

    // QuoteLines are an independent aggregate root (see QuoteLine.cs) with no FK relationship
    // configured, so deleting a quote doesn't cascade automatically - same pattern as
    // CustomerAppService.DeleteAsync. Blocked instead if an Order was already converted from this
    // Quote (system-wide "block deletion if dependents exist" policy, see DependencyGuard).
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _orderRepository.GetListAsync(x => x.QuoteId == id)).Count, "Order")
        );

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

        var salespersonIds = quotes
            .Where(x => x.SalespersonUserId.HasValue)
            .Select(x => x.SalespersonUserId!.Value)
            .Distinct()
            .ToList();
        var salespersonNamesById = salespersonIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => salespersonIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var quote in quotes)
        {
            if (namesById.TryGetValue(quote.CustomerId, out var customerName))
            {
                quote.CustomerName = customerName;
            }

            var lines = linesByQuoteId[quote.Id].ToList();
            quote.Subtotal = lines.Sum(x => x.Subtotal());
            quote.TaxAmount = lines.Sum(x => x.TaxAmount());
            quote.Total = quote.Subtotal + quote.TaxAmount;

            if (quote.ProposalId.HasValue && proposalNumbersById.TryGetValue(quote.ProposalId.Value, out var proposalNumber))
            {
                quote.ProposalNumber = proposalNumber;
            }

            if (quote.SalespersonUserId.HasValue && salespersonNamesById.TryGetValue(quote.SalespersonUserId.Value, out var salespersonName))
            {
                quote.SalespersonName = salespersonName;
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
        entity.SalespersonUserId = await ResolveSalespersonUserIdAsync(createInput.SalespersonUserId, createInput.ProposalId);
        return entity;
    }

    // Purely attributive (commission/reporting) - see Quote.SalespersonUserId's own comment.
    // Respects an explicit value if one is ever passed in; otherwise walks Proposal -> Opportunity
    // for the Proposal->Quote path (the standalone Create page's optional ProposalId field), else
    // falls back to whoever is creating the Quote directly.
    private async Task<Guid?> ResolveSalespersonUserIdAsync(Guid? explicitValue, Guid? proposalId)
    {
        if (explicitValue.HasValue)
        {
            return explicitValue;
        }

        if (proposalId.HasValue)
        {
            var proposal = await _proposalRepository.FindAsync(proposalId.Value);
            if (proposal != null)
            {
                var opportunity = await _opportunityRepository.FindAsync(proposal.OpportunityId);
                if (opportunity?.AssignedToUserId != null)
                {
                    return opportunity.AssignedToUserId;
                }
            }
        }

        return CurrentUser.Id;
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
        var previousCustomerId = entity.CustomerId;

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

        if (previousCustomerId != entity.CustomerId)
        {
            await RepriceLinesForNewCustomerAsync(entity);
        }
    }

    // Changing a Quote's Customer re-resolves every existing line's UnitPrice/DiscountPercent
    // against the new customer's price list/discount default - the one place in the sales-document
    // pricing flow that deliberately overwrites a value a user may have hand-edited, since
    // "change the customer" is explicitly meant to trigger a refresh rather than leave stale
    // pricing from the old customer sitting on the document.
    private async Task RepriceLinesForNewCustomerAsync(Quote quote)
    {
        var customer = await _customerRepository.FindAsync(quote.CustomerId);
        if (customer == null)
        {
            return;
        }

        var priceListId = quote.PriceListId ?? customer.DefaultPriceListId;
        var lines = await _lineRepository.GetListAsync(x => x.QuoteId == quote.Id && x.ProductId != null);
        if (lines.Count == 0)
        {
            return;
        }

        foreach (var line in lines)
        {
            line.UnitPrice = await PriceListResolver.ResolveUnitPriceAsync(
                _priceListItemRepository, _productRepository, priceListId, line.ProductId!.Value);
            line.DiscountPercent = customer.DiscountPercent;
        }

        await _lineRepository.UpdateManyAsync(lines);
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
        entity.PriceListId = input.PriceListId;
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
            SalespersonUserId = quote.SalespersonUserId,
            // Re-resolved at the Order's own creation date rather than copying the Quote's
            // snapshot - a Quote issued in January converting to an Order in March should reflect
            // March's rate, not January's. If no rate is available, defaults to 1:1 (no conversion).
            ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, quote.CurrencyCode, orderDate, throwIfNotFound: false)
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
