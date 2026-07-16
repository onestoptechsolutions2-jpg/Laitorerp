using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class OrderAppService :
    CrudAppService<Order, OrderDto, Guid, GetOrderListInput, CreateUpdateOrderDto>
{
    private readonly IRepository<OrderLine, Guid> _lineRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;

    public OrderAppService(
        IRepository<Order, Guid> repository,
        IRepository<OrderLine, Guid> lineRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Create;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Delete;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

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

            order.Total = linesByOrderId[order.Id]
                .Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
        }
    }

    protected override async Task<Order> MapToEntityAsync(CreateUpdateOrderDto createInput)
    {
        var count = await Repository.GetCountAsync();
        var orderNumber = $"SO-{count + 1:D6}";

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
    }

    // The concrete mechanism behind "order becomes an invoice" - carries line items and pricing
    // forward instead of the user re-entering them. Due date defaults to 30 days out; adjustable
    // afterwards on the invoice itself.
    public async Task<InvoiceDto> ConvertToInvoiceAsync(Guid orderId)
    {
        await CheckCreatePolicyAsync();

        var order = await Repository.GetAsync(orderId);
        var orderLines = await _lineRepository.GetListAsync(x => x.OrderId == orderId);

        var invoiceCount = await _invoiceRepository.GetCountAsync();
        var invoiceNumber = $"INV-{invoiceCount + 1:D6}";

        var invoice = new Invoice(GuidGenerator.Create(), order.CustomerId, invoiceNumber)
        {
            OrderId = order.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = Clock.Now,
            DueDate = Clock.Now.AddDays(30),
            Notes = order.Notes
        };
        await _invoiceRepository.InsertAsync(invoice, autoSave: true);

        foreach (var orderLine in orderLines)
        {
            var invoiceLine = new InvoiceLine(GuidGenerator.Create(), invoice.Id, orderLine.Description, orderLine.UnitPrice)
            {
                ProductId = orderLine.ProductId,
                Quantity = orderLine.Quantity,
                DiscountPercent = orderLine.DiscountPercent
            };
            await _invoiceLineRepository.InsertAsync(invoiceLine, autoSave: true);
        }

        return ObjectMapper.Map<Invoice, InvoiceDto>(invoice);
    }
}
