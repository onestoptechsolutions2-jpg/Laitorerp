using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class TaxRateAppService :
    CrudAppService<TaxRate, TaxRateDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateTaxRateDto>
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<QuoteLine, Guid> _quoteLineRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;
    private readonly IRepository<SupplierInvoiceLine, Guid> _supplierInvoiceLineRepository;
    private readonly IRepository<PosSaleLine, Guid> _posSaleLineRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public TaxRateAppService(
        IRepository<TaxRate, Guid> repository,
        IRepository<Product, Guid> productRepository,
        IRepository<QuoteLine, Guid> quoteLineRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<SupplierInvoiceLine, Guid> supplierInvoiceLineRepository,
        IRepository<PosSaleLine, Guid> posSaleLineRepository,
        IRepository<Vendor, Guid> vendorRepository)
        : base(repository)
    {
        _productRepository = productRepository;
        _quoteLineRepository = quoteLineRepository;
        _orderLineRepository = orderLineRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _supplierInvoiceLineRepository = supplierInvoiceLineRepository;
        _posSaleLineRepository = posSaleLineRepository;
        _vendorRepository = vendorRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _productRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Product"),
            (async () => (await _quoteLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Quote Line"),
            (async () => (await _orderLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Order Line"),
            (async () => (await _invoiceLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Invoice Line"),
            (async () => (await _purchaseOrderLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Purchase Order Line"),
            (async () => (await _supplierInvoiceLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "Supplier Invoice Line"),
            (async () => (await _posSaleLineRepository.GetListAsync(x => x.TaxRateId == id)).Count, "POS Sale Line"),
            (async () => (await _vendorRepository.GetListAsync(x => x.WithholdingTaxRateId == id)).Count, "Vendor")
        );

        await Repository.DeleteAsync(id);
    }

    // CreateUpdateTaxRateDto -> TaxRate is mapped manually rather than via Mapperly - same reason
    // as every other entity in this app (protected Id setter).
    protected override async Task<TaxRate> MapToEntityAsync(CreateUpdateTaxRateDto createInput)
    {
        if (createInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(createInput.TaxType, currentId: null);
        }

        var entity = new TaxRate(GuidGenerator.Create(), createInput.Name, createInput.Percent);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateTaxRateDto updateInput, TaxRate entity)
    {
        if (updateInput.IsDefault)
        {
            await ClearOtherDefaultsAsync(updateInput.TaxType, currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Keeps "the default tax rate" unambiguous per TaxType, since it's what every line without its
    // own rate falls back to - same pattern as ProductVendorAppService.ClearOtherPreferredVendorsAsync.
    // Scoped to TaxType (not global) so a default VAT rate and a default withholding rate can
    // coexist without stealing each other's default flag.
    private async Task ClearOtherDefaultsAsync(TaxType taxType, Guid? currentId)
    {
        var others = await Repository.GetListAsync(x => x.IsDefault && x.TaxType == taxType && x.Id != (currentId ?? Guid.Empty));

        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        if (others.Count > 0)
        {
            await Repository.UpdateManyAsync(others);
        }
    }

    private static void CopyToEntity(CreateUpdateTaxRateDto input, TaxRate entity)
    {
        entity.Name = input.Name;
        entity.Percent = input.Percent;
        entity.IsDefault = input.IsDefault;
        entity.TaxType = input.TaxType;
    }
}
