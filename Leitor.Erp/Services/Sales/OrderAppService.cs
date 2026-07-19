using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class OrderAppService :
    CrudAppService<Order, OrderDto, Guid, GetOrderListInput, CreateUpdateOrderDto>
{
    private readonly IRepository<OrderLine, Guid> _lineRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;
    private readonly IRepository<FieldServiceJob, Guid> _fieldServiceJobRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public OrderAppService(
        IRepository<Order, Guid> repository,
        IRepository<OrderLine, Guid> lineRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IRepository<FieldServiceJob, Guid> fieldServiceJobRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _customerRepository = customerRepository;
        _quoteRepository = quoteRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _milestoneRepository = milestoneRepository;
        _taxRateRepository = taxRateRepository;
        _stageEventRepository = stageEventRepository;
        _fieldServiceJobRepository = fieldServiceJobRepository;
        _dataFilter = dataFilter;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Create;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Order", id);

        var lines = await _lineRepository.GetListAsync(x => x.OrderId == id);
        await _lineRepository.DeleteManyAsync(lines);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Order>> CreateFilteredQueryAsync(GetOrderListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.OrderNumber.Contains(input.Filter!));
    }

    public override async Task<OrderDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<OrderDto>> GetListAsync(GetOrderListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<OrderDto> orders)
    {
        var customerIds = orders.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var namesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var orderIds = orders.Select(x => x.Id).ToList();
        var allLines = await _lineRepository.GetListAsync(x => orderIds.Contains(x.OrderId));
        var linesByOrderId = allLines.ToLookup(x => x.OrderId);

        var quoteIds = orders
            .Where(x => x.QuoteId.HasValue)
            .Select(x => x.QuoteId!.Value)
            .Distinct()
            .ToList();
        var quoteNumbersById = quoteIds.Count > 0
            ? (await _quoteRepository.GetListAsync(x => quoteIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.QuoteNumber)
            : new Dictionary<Guid, string>();

        foreach (var order in orders)
        {
            if (namesById.TryGetValue(order.CustomerId, out var customerName))
            {
                order.CustomerName = customerName;
            }

            if (order.QuoteId.HasValue && quoteNumbersById.TryGetValue(order.QuoteId.Value, out var quoteNumber))
            {
                order.QuoteNumber = quoteNumber;
            }

            var lines = linesByOrderId[order.Id].ToList();
            order.Subtotal = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
            order.TaxAmount = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m) * x.TaxRatePercent / 100m);
            order.Total = order.Subtotal + order.TaxAmount;
        }
    }

    protected override async Task<Order> MapToEntityAsync(CreateUpdateOrderDto createInput)
    {
        var orderNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "SO-");

        var entity = new Order(GuidGenerator.Create(), createInput.CustomerId, orderNumber);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateOrderDto updateInput, Order entity)
    {
        // Once an Order leaves Submitted it's locked - editing requires an explicit unlock first
        // (see UnlockForRevisionAsync). Single-use: consumed the moment this edit is saved.
        if (entity.IsLocked && entity.UnlockedByUserId == null)
        {
            throw new UserFriendlyException("This order is locked because it's no longer awaiting submission. Unlock it for revision before making changes.");
        }

        var wasUnlocked = entity.UnlockedByUserId != null;
        var wasConfirmed = entity.Status == OrderStatus.Confirmed;

        CopyToEntity(updateInput, entity);
        entity.Version++;

        if (wasUnlocked)
        {
            entity.UnlockedByUserId = null;
            entity.UnlockedAt = null;
            entity.UnlockReason = null;
        }

        if (!wasConfirmed && entity.Status == OrderStatus.Confirmed)
        {
            await OnOrderConfirmedAsync(entity);
        }
    }

    // Only a holder of Sales.Unlock (Ops Manager) can unlock an approved Order for revision.
    public async Task UnlockForRevisionAsync(Guid id, string reason)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Unlock);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("A reason is required to unlock this order for revision.");
        }

        var entity = await Repository.GetAsync(id);
        entity.UnlockedByUserId = CurrentUser.Id;
        entity.UnlockedAt = Clock.Now;
        entity.UnlockReason = reason;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", entity.Id, WorkflowStage.Unlocked, notes: reason);
    }

    // Fired the moment an Order's Status transitions into Confirmed (see MapToEntityAsync above).
    // Only Milestone-terms orders get an automatic deposit - a Net30/DueOnReceipt/etc order is
    // meant to be invoiced in full via ConvertToInvoiceAsync whenever the user is ready, not split
    // into a deposit it never asked for.
    private async Task OnOrderConfirmedAsync(Order order)
    {
        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", order.Id, WorkflowStage.OrderConfirmed);

        if (order.PaymentTerms != PaymentTerms.Milestone)
        {
            return;
        }

        var hasMilestones = (await _milestoneRepository.GetListAsync(x => x.OrderId == order.Id)).Any();
        if (hasMilestones)
        {
            return;
        }

        var lines = await _lineRepository.GetListAsync(x => x.OrderId == order.Id);
        var subtotal = lines.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));

        var deposit = new OrderPaymentMilestone(GuidGenerator.Create(), order.Id, "Deposit", 50)
        {
            Kind = OrderPaymentMilestoneKind.Deposit
        };
        await _milestoneRepository.InsertAsync(deposit, autoSave: true);

        await CreateInvoiceForMilestoneAsync(order, deposit, subtotal);
        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", order.Id, WorkflowStage.DepositInvoiceIssued);
    }

    // Shared by the automatic deposit trigger above and ConvertMilestoneToInvoiceAsync below.
    private async Task<Invoice> CreateInvoiceForMilestoneAsync(Order order, OrderPaymentMilestone milestone, decimal orderSubtotal)
    {
        var defaultTaxRate = (await _taxRateRepository.GetListAsync(x => x.IsDefault)).FirstOrDefault();

        var invoiceNumber = await DocumentNumbering.NextAsync(_invoiceRepository, _dataFilter, "INV-");
        var issueDate = Clock.Now;

        var invoice = new Invoice(GuidGenerator.Create(), order.CustomerId, invoiceNumber)
        {
            OrderId = order.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = issueDate,
            DueDate = issueDate,
            Notes = order.Notes,
            PaymentTerms = order.PaymentTerms
        };
        await _invoiceRepository.InsertAsync(invoice, autoSave: true);

        var milestoneAmount = orderSubtotal * milestone.Percent / 100m;
        var invoiceLine = new InvoiceLine(
            GuidGenerator.Create(),
            invoice.Id,
            $"{milestone.Description} ({milestone.Percent:N0}% of Order Total)",
            milestoneAmount)
        {
            TaxRateId = defaultTaxRate?.Id,
            TaxRatePercent = defaultTaxRate?.Percent ?? 0
        };
        await _invoiceLineRepository.InsertAsync(invoiceLine, autoSave: true);

        milestone.IsInvoiced = true;
        milestone.InvoiceId = invoice.Id;
        await _milestoneRepository.UpdateAsync(milestone, autoSave: true);

        return invoice;
    }

    private static void CopyToEntity(CreateUpdateOrderDto input, Order entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.QuoteId = input.QuoteId;
        entity.Status = input.Status;
        entity.OrderDate = input.OrderDate;
        entity.Notes = input.Notes;
        entity.PaymentTerms = input.PaymentTerms;
    }

    // The concrete mechanism behind "order becomes an invoice" - carries line items and pricing
    // forward instead of the user re-entering them. Due date defaults per the order's PaymentTerms
    // (PaymentTermsCalculator.DueDate); adjustable afterwards on the invoice itself. Milestone
    // orders can't be invoiced in full - see ConvertMilestoneToInvoiceAsync. Requires the order to
    // have actually been confirmed (Deposit Invoice/full invoice only ever originates from a
    // confirmed Sales Order) and blocks invoicing the same order twice via this path.
    public async Task<InvoiceDto> ConvertToInvoiceAsync(Guid orderId)
    {
        await CheckCreatePolicyAsync();

        var order = await Repository.GetAsync(orderId);

        if (order.PaymentTerms == PaymentTerms.Milestone)
        {
            throw new UserFriendlyException("This order is billed by milestone - invoice each milestone individually instead of converting the whole order.");
        }

        if (order.Status is not (OrderStatus.Confirmed or OrderStatus.Fulfilled))
        {
            throw new UserFriendlyException("This order must be confirmed before it can be invoiced.");
        }

        var alreadyInvoiced = (await _invoiceRepository.GetListAsync(x => x.OrderId == order.Id)).Any();
        if (alreadyInvoiced)
        {
            throw new UserFriendlyException("This order has already been invoiced.");
        }

        var linkedJobs = await _fieldServiceJobRepository.GetListAsync(x => x.OrderId == orderId);
        if (linkedJobs.Count > 0 && linkedJobs.Any(x => x.Status != FieldServiceJobStatus.Completed))
        {
            throw new UserFriendlyException("All field service jobs for this order must be completed before it can be invoiced.");
        }

        var orderLines = await _lineRepository.GetListAsync(x => x.OrderId == orderId);

        var invoiceNumber = await DocumentNumbering.NextAsync(_invoiceRepository, _dataFilter, "INV-");
        var issueDate = Clock.Now;

        var invoice = new Invoice(GuidGenerator.Create(), order.CustomerId, invoiceNumber)
        {
            OrderId = order.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = issueDate,
            DueDate = PaymentTermsCalculator.DueDate(issueDate, order.PaymentTerms),
            Notes = order.Notes,
            PaymentTerms = order.PaymentTerms
        };
        await _invoiceRepository.InsertAsync(invoice, autoSave: true);

        foreach (var orderLine in orderLines)
        {
            var invoiceLine = new InvoiceLine(GuidGenerator.Create(), invoice.Id, orderLine.Description, orderLine.UnitPrice)
            {
                ProductId = orderLine.ProductId,
                Quantity = orderLine.Quantity,
                DiscountPercent = orderLine.DiscountPercent,
                TaxRateId = orderLine.TaxRateId,
                TaxRatePercent = orderLine.TaxRatePercent
            };
            await _invoiceLineRepository.InsertAsync(invoiceLine, autoSave: true);
        }

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", order.Id, WorkflowStage.FinalInvoiceIssued);

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }

    // The Milestone-terms counterpart to ConvertToInvoiceAsync: bills one planned percentage of
    // the order total as a single invoice line, rather than the full order at once. Taxed at the
    // system default TaxRate rather than a line-weighted blend of the order's own lines - see the
    // "Deliberate scope cuts" note in the Phase 2 plan. A Kind: Final milestone additionally
    // requires every FieldServiceJob linked to this order to be Completed first - "final invoice
    // only after installation," skipped entirely for orders with no linked jobs (pure product
    // sales, nothing to install).
    public async Task<InvoiceDto> ConvertMilestoneToInvoiceAsync(Guid orderId, Guid milestoneId)
    {
        await CheckCreatePolicyAsync();

        var order = await Repository.GetAsync(orderId);
        var milestone = await _milestoneRepository.GetAsync(milestoneId);

        if (milestone.OrderId != orderId)
        {
            throw new UserFriendlyException("This milestone does not belong to the specified order.");
        }

        if (milestone.IsInvoiced)
        {
            throw new UserFriendlyException("This milestone has already been invoiced.");
        }

        if (milestone.Kind == OrderPaymentMilestoneKind.Final)
        {
            var linkedJobs = await _fieldServiceJobRepository.GetListAsync(x => x.OrderId == orderId);
            if (linkedJobs.Count > 0 && linkedJobs.Any(x => x.Status != FieldServiceJobStatus.Completed))
            {
                throw new UserFriendlyException("All field service jobs for this order must be completed before the final invoice can be issued.");
            }
        }

        var orderDto = await GetAsync(orderId);
        var invoice = await CreateInvoiceForMilestoneAsync(order, milestone, orderDto.Subtotal);

        if (milestone.Kind == OrderPaymentMilestoneKind.Final)
        {
            await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", order.Id, WorkflowStage.FinalInvoiceIssued);
        }
        else if (milestone.Kind == OrderPaymentMilestoneKind.Deposit)
        {
            await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Order", order.Id, WorkflowStage.DepositInvoiceIssued);
        }

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }
}
