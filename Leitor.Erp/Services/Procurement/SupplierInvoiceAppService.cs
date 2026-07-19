using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class SupplierInvoiceAppService :
    CrudAppService<SupplierInvoice, SupplierInvoiceDto, Guid, GetSupplierInvoiceListInput, CreateUpdateSupplierInvoiceDto>
{
    private readonly IRepository<SupplierInvoiceLine, Guid> _lineRepository;
    private readonly IRepository<VendorPayment, Guid> _paymentRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public SupplierInvoiceAppService(
        IRepository<SupplierInvoice, Guid> repository,
        IRepository<SupplierInvoiceLine, Guid> lineRepository,
        IRepository<VendorPayment, Guid> paymentRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _lineRepository = lineRepository;
        _paymentRepository = paymentRepository;
        _vendorRepository = vendorRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
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

            invoice.PaymentStatus = ComputePaymentStatus(invoice, now);
        }
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

    protected override Task<SupplierInvoice> MapToEntityAsync(CreateUpdateSupplierInvoiceDto createInput)
    {
        var entity = new SupplierInvoice(GuidGenerator.Create(), createInput.PurchaseOrderId, createInput.VendorId, createInput.SupplierInvoiceNumber);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateSupplierInvoiceDto updateInput, SupplierInvoice entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
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
    }
}
