using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class InvoiceLineAppService :
    CrudAppService<InvoiceLine, InvoiceLineDto, Guid, GetInvoiceLineListInput, CreateUpdateInvoiceLineDto>
{
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Product, Guid> _productRepository;

    public InvoiceLineAppService(
        IRepository<InvoiceLine, Guid> repository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository)
        : base(repository)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<InvoiceLine>> CreateFilteredQueryAsync(GetInvoiceLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.InvoiceId.HasValue, x => x.InvoiceId == input.InvoiceId!.Value);
    }

    public override async Task<PagedResultDto<InvoiceLineDto>> GetListAsync(GetInvoiceLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<InvoiceLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(InvoiceLineDto dto)
    {
        dto.LineTotal = dto.UnitPrice * dto.Quantity * (1 - dto.DiscountPercent / 100m);
    }

    protected override async Task<InvoiceLine> MapToEntityAsync(CreateUpdateInvoiceLineDto createInput)
    {
        var entity = new InvoiceLine(GuidGenerator.Create(), createInput.InvoiceId, createInput.Description, createInput.UnitPrice);
        await CopyToEntityAsync(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateInvoiceLineDto updateInput, InvoiceLine entity)
    {
        await CopyToEntityAsync(updateInput, entity);
    }

    private async Task CopyToEntityAsync(CreateUpdateInvoiceLineDto input, InvoiceLine entity)
    {
        entity.InvoiceId = input.InvoiceId;
        entity.ProductId = input.ProductId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;

        (entity.TaxRateId, entity.TaxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, input.TaxRateId, input.ProductId);
    }
}
