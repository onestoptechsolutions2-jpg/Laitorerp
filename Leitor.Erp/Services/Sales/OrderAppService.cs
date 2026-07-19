using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
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
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public OrderAppService(
        IRepository<Order, Guid> repository,
        IRepository<OrderLine, Guid> lineRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _milestoneRepository = milestoneRepository;
        _taxRateRepository = taxRateRepository;
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

        foreach (var order in orders)
        {
            if (namesById.TryGetValue(order.CustomerId, out var customerName))
            {
                order.CustomerName = customerName;
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

    protected override Task MapToEntityAsync(CreateUpdateOrderDto updateInput, Order entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
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
    // orders can't be invoiced in full - see ConvertMilestoneToInvoiceAsync.
    public async Task<InvoiceDto> ConvertToInvoiceAsync(Guid orderId)
    {
        await CheckCreatePolicyAsync();

        var order = await Repository.GetAsync(orderId);

        if (order.PaymentTerms == PaymentTerms.Milestone)
        {
            throw new UserFriendlyException("This order is billed by milestone - invoice each milestone individually instead of converting the whole order.");
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

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }

    // The Milestone-terms counterpart to ConvertToInvoiceAsync: bills one planned percentage of
    // the order total as a single invoice line, rather than the full order at once. Taxed at the
    // system default TaxRate rather than a line-weighted blend of the order's own lines - see the
    // "Deliberate scope cuts" note in the Phase 2 plan.
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

        var orderDto = await GetAsync(orderId);

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

        var milestoneAmount = orderDto.Subtotal * milestone.Percent / 100m;
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

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }
}
