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

public class InvoiceAppService :
    CrudAppService<Invoice, InvoiceDto, Guid, GetInvoiceListInput, CreateUpdateInvoiceDto>
{
    private readonly IRepository<InvoiceLine, Guid> _lineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public InvoiceAppService(
        IRepository<Invoice, Guid> repository,
        IRepository<InvoiceLine, Guid> lineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<Customer, Guid> customerRepository,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _paymentRepository = paymentRepository;
        _customerRepository = customerRepository;
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

        var now = Clock.Now;

        foreach (var invoice in invoices)
        {
            if (namesById.TryGetValue(invoice.CustomerId, out var customerName))
            {
                invoice.CustomerName = customerName;
            }

            invoice.Total = linesByInvoiceId[invoice.Id]
                .Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m));
            invoice.AmountPaid = paymentsByInvoiceId[invoice.Id].Sum(x => x.Amount);

            invoice.PaymentStatus = ComputePaymentStatus(invoice, now);
        }
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
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateInvoiceDto updateInput, Invoice entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateInvoiceDto input, Invoice entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.OrderId = input.OrderId;
        entity.Status = input.Status;
        entity.IssueDate = input.IssueDate;
        entity.DueDate = input.DueDate;
        entity.Notes = input.Notes;
    }
}
