using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class PurchaseOrderLineAppService :
    CrudAppService<PurchaseOrderLine, PurchaseOrderLineDto, Guid, GetPurchaseOrderLineListInput, CreateUpdatePurchaseOrderLineDto>
{
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<ProductVendor, Guid> _productVendorRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public PurchaseOrderLineAppService(
        IRepository<PurchaseOrderLine, Guid> repository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<ProductVendor, Guid> productVendorRepository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository)
        : base(repository)
    {
        _purchaseOrderRepository = purchaseOrderRepository;
        _productVendorRepository = productVendorRepository;
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;

        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<PurchaseOrderLine>> CreateFilteredQueryAsync(GetPurchaseOrderLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.PurchaseOrderId.HasValue, x => x.PurchaseOrderId == input.PurchaseOrderId!.Value);
    }

    public override async Task<PagedResultDto<PurchaseOrderLineDto>> GetListAsync(GetPurchaseOrderLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<PurchaseOrderLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(PurchaseOrderLineDto dto)
    {
        dto.LineTotal = dto.Subtotal();
    }

    protected override async Task<PurchaseOrderLine> MapToEntityAsync(CreateUpdatePurchaseOrderLineDto createInput)
    {
        var entity = new PurchaseOrderLine(GuidGenerator.Create(), createInput.PurchaseOrderId, createInput.Description, createInput.UnitPrice);
        await CopyToEntityAsync(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdatePurchaseOrderLineDto updateInput, PurchaseOrderLine entity)
    {
        await CopyToEntityAsync(updateInput, entity);
    }

    private async Task CopyToEntityAsync(CreateUpdatePurchaseOrderLineDto input, PurchaseOrderLine entity)
    {
        entity.PurchaseOrderId = input.PurchaseOrderId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;

        // UnitPrice == 0 means the form's untouched default - resolve the vendor's own sourcing
        // cost for this product (same (VendorId, ProductId) -> ProductVendor.Cost lookup the
        // "Create PO from Sales Order" dropship flow already does), generalized to every PO's
        // normal manual add-line path. Same tradeoff as the Sales-side PriceListResolver: an
        // explicit 0 gets re-resolved too.
        if (entity.UnitPrice == 0 && input.ProductId.HasValue)
        {
            var purchaseOrder = await _purchaseOrderRepository.FindAsync(input.PurchaseOrderId);
            if (purchaseOrder != null)
            {
                var productVendor = (await _productVendorRepository.GetListAsync(
                    x => x.VendorId == purchaseOrder.VendorId && x.ProductId == input.ProductId.Value)).FirstOrDefault();
                if (productVendor != null)
                {
                    entity.UnitPrice = productVendor.Cost;
                }
            }
        }

        (entity.TaxRateId, entity.TaxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, input.TaxRateId, input.ProductId);
    }
}