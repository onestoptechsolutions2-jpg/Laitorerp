using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class ProductAppService :
    CrudAppService<Product, ProductDto, Guid, GetProductListInput, CreateUpdateProductDto>
{
    private readonly IRepository<ProductCategory, Guid> _categoryRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly IRepository<ProductVendor, Guid> _productVendorRepository;
    private readonly IRepository<ProductBundleItem, Guid> _bundleItemRepository;
    private readonly IRepository<QuoteLine, Guid> _quoteLineRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<PriceListItem, Guid> _priceListItemRepository;
    private readonly IRepository<PosSaleLine, Guid> _posSaleLineRepository;
    private readonly IRepository<FieldServiceJobPart, Guid> _jobPartRepository;

    public ProductAppService(
        IRepository<Product, Guid> repository,
        IRepository<ProductCategory, Guid> categoryRepository,
        IRepository<StockMovement, Guid> stockMovementRepository,
        IRepository<ProductVendor, Guid> productVendorRepository,
        IRepository<ProductBundleItem, Guid> bundleItemRepository,
        IRepository<QuoteLine, Guid> quoteLineRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<PriceListItem, Guid> priceListItemRepository,
        IRepository<PosSaleLine, Guid> posSaleLineRepository,
        IRepository<FieldServiceJobPart, Guid> jobPartRepository)
        : base(repository)
    {
        _categoryRepository = categoryRepository;
        _stockMovementRepository = stockMovementRepository;
        _productVendorRepository = productVendorRepository;
        _bundleItemRepository = bundleItemRepository;
        _quoteLineRepository = quoteLineRepository;
        _orderLineRepository = orderLineRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _priceListItemRepository = priceListItemRepository;
        _posSaleLineRepository = posSaleLineRepository;
        _jobPartRepository = jobPartRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Create;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Delete;
    }

    // ProductVendor sourcing rows and this product's own ProductBundleItem composition (if it's a
    // bundle) have no independent identity, so they're still cascaded. Every line-item table that
    // can reference this Product as a sold/bought/stocked item, plus another bundle using it as a
    // component, are independent records - blocked instead (system-wide "block deletion if
    // dependents exist" policy, see DependencyGuard).
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _bundleItemRepository.GetListAsync(x => x.ComponentProductId == id)).Count, "Bundle using it as a component"),
            (async () => (await _quoteLineRepository.GetListAsync(x => x.ProductId == id)).Count, "Quote Line"),
            (async () => (await _orderLineRepository.GetListAsync(x => x.ProductId == id)).Count, "Order Line"),
            (async () => (await _invoiceLineRepository.GetListAsync(x => x.ProductId == id)).Count, "Invoice Line"),
            (async () => (await _purchaseOrderLineRepository.GetListAsync(x => x.ProductId == id)).Count, "Purchase Order Line"),
            (async () => (await _supplierInvoiceLineRepository.GetListAsync(x => x.ProductId == id)).Count, "Supplier Invoice Line"),
            (async () => (await _priceListItemRepository.GetListAsync(x => x.ProductId == id)).Count, "Price List Item"),
            (async () => (await _posSaleLineRepository.GetListAsync(x => x.ProductId == id)).Count, "POS Sale Line"),
            (async () => (await _jobPartRepository.GetListAsync(x => x.ProductId == id)).Count, "Field Service Job Part"),
            (async () => (await _stockMovementRepository.GetListAsync(x => x.ProductId == id)).Count, "Stock Movement")
        );

        var productVendors = await _productVendorRepository.GetListAsync(x => x.ProductId == id);
        await _productVendorRepository.DeleteManyAsync(productVendors);

        var bundleComposition = await _bundleItemRepository.GetListAsync(x => x.BundleProductId == id);
        await _bundleItemRepository.DeleteManyAsync(bundleComposition);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Product>> CreateFilteredQueryAsync(GetProductListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive!.Value)
            .WhereIf(input.CategoryId.HasValue, x => x.CategoryId == input.CategoryId!.Value);
    }

    public override async Task<ProductDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<ProductDto> products)
    {
        var categoryIds = products.Where(x => x.CategoryId.HasValue).Select(x => x.CategoryId!.Value).Distinct().ToList();
        var namesById = categoryIds.Count > 0
            ? (await _categoryRepository.GetListAsync(x => categoryIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var trackedProductIds = products.Where(x => x.TrackInventory).Select(x => x.Id).ToList();
        var onHandByProductId = trackedProductIds.Count > 0
            ? (await _stockMovementRepository.GetListAsync(x => trackedProductIds.Contains(x.ProductId)))
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity))
            : new Dictionary<Guid, decimal>();

        foreach (var product in products)
        {
            if (product.CategoryId.HasValue && namesById.TryGetValue(product.CategoryId.Value, out var categoryName))
            {
                product.CategoryName = categoryName;
            }

            if (product.TrackInventory)
            {
                product.QuantityOnHand = onHandByProductId.GetValueOrDefault(product.Id);
            }
        }
    }

    // CreateUpdateProductDto -> Product is mapped manually rather than via Mapperly - same reason
    // as every other entity in this app: Product's Id has a protected setter and its constructor
    // needs a generated Guid the DTO has no source for.
    protected override Task<Product> MapToEntityAsync(CreateUpdateProductDto createInput)
    {
        var entity = new Product(GuidGenerator.Create(), createInput.Name, createInput.UnitPrice);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateProductDto updateInput, Product entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateProductDto input, Product entity)
    {
        entity.Name = input.Name;
        entity.Sku = input.Sku;
        entity.Barcode = input.Barcode;
        entity.Description = input.Description;
        entity.Type = input.Type;
        entity.UnitPrice = input.UnitPrice;
        entity.IsActive = input.IsActive;
        entity.Cost = input.Cost;
        entity.TaxRateId = input.TaxRateId;
        entity.CategoryId = input.CategoryId;
        entity.IsBundle = input.IsBundle;
        entity.TrackInventory = input.TrackInventory;
        entity.ReorderPoint = input.ReorderPoint;
        entity.ReorderQuantity = input.ReorderQuantity;
    }
}
