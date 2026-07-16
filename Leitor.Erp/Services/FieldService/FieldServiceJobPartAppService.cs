using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.FieldService;

public class FieldServiceJobPartAppService :
    CrudAppService<FieldServiceJobPart, FieldServiceJobPartDto, Guid, GetFieldServiceJobPartListInput, CreateUpdateFieldServiceJobPartDto>
{
    public FieldServiceJobPartAppService(IRepository<FieldServiceJobPart, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.FieldService.Default;
        GetListPolicyName = ErpPermissions.FieldService.Default;
        CreatePolicyName = ErpPermissions.FieldService.Edit;
        UpdatePolicyName = ErpPermissions.FieldService.Edit;
        DeletePolicyName = ErpPermissions.FieldService.Edit;
    }

    protected override async Task<IQueryable<FieldServiceJobPart>> CreateFilteredQueryAsync(GetFieldServiceJobPartListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.JobId.HasValue, x => x.JobId == input.JobId!.Value);
    }

    protected override Task<FieldServiceJobPart> MapToEntityAsync(CreateUpdateFieldServiceJobPartDto createInput)
    {
        var entity = new FieldServiceJobPart(GuidGenerator.Create(), createInput.JobId, createInput.Description)
        {
            ProductId = createInput.ProductId,
            Quantity = createInput.Quantity
        };
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateFieldServiceJobPartDto updateInput, FieldServiceJobPart entity)
    {
        entity.JobId = updateInput.JobId;
        entity.ProductId = updateInput.ProductId;
        entity.Description = updateInput.Description;
        entity.Quantity = updateInput.Quantity;
        return Task.CompletedTask;
    }
}
