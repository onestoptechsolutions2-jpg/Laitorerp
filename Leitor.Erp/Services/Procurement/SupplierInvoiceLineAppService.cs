using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Sales;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class SupplierInvoiceLineAppService :
    CrudAppService<SupplierInvoiceLine, SupplierInvoiceLineDto, Guid, GetSupplierInvoiceLineListInput, CreateUpdateSupplierInvoiceLineDto>
{
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public SupplierInvoiceLineAppService(
        IRepository<SupplierInvoiceLine, Guid> repository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository)
        : base(repository)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;

        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<SupplierInvoiceLine>> CreateFilteredQueryAsync(GetSupplierInvoiceLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.SupplierInvoiceId.HasValue, x => x.SupplierInvoiceId == input.SupplierInvoiceId!.Value);
    }

    public override async Task<PagedResultDto<SupplierInvoiceLineDto>> GetListAsync(GetSupplierInvoiceLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<SupplierInvoiceLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(SupplierInvoiceLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
    }

    protected override async Task<SupplierInvoiceLine> MapToEntityAsync(CreateUpdateSupplierInvoiceLineDto createInput)
    {
        var entity = new SupplierInvoiceLine(GuidGenerator.Create(), createInput.SupplierInvoiceId, createInput.Description, createInput.UnitPrice);
        await CopyToEntityAsync(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateSupplierInvoiceLineDto updateInput, SupplierInvoiceLine entity)
    {
        await CopyToEntityAsync(updateInput, entity);
    }

    private async Task CopyToEntityAsync(CreateUpdateSupplierInvoiceLineDto input, SupplierInvoiceLine entity)
    {
        entity.SupplierInvoiceId = input.SupplierInvoiceId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;

        (entity.TaxRateId, entity.TaxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, input.TaxRateId, input.ProductId);
    }
}