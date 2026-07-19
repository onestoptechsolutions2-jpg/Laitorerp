using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace Leitor.Erp.Services.Sales;

// Not a CrudAppService: milestones are never edited once added, only added/deleted/invoiced -
// same shape as NeedsAssessmentAttachmentAppService.
public class OrderPaymentMilestoneAppService : ErpAppService
{
    private readonly IRepository<OrderPaymentMilestone, Guid> _repository;

    public OrderPaymentMilestoneAppService(IRepository<OrderPaymentMilestone, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<List<OrderPaymentMilestoneDto>> GetListAsync(Guid orderId)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Default);

        var milestones = await _repository.GetListAsync(x => x.OrderId == orderId);
        return milestones
            .OrderBy(x => x.CreationTime)
            .Select(x => ObjectMapper.Map<OrderPaymentMilestone, OrderPaymentMilestoneDto>(x))
            .ToList();
    }

    public async Task<OrderPaymentMilestoneDto> CreateAsync(CreateUpdateOrderPaymentMilestoneDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Edit);

        var entity = new OrderPaymentMilestone(GuidGenerator.Create(), input.OrderId, input.Description, input.Percent);
        await _repository.InsertAsync(entity, autoSave: true);

        return ObjectMapper.Map<OrderPaymentMilestone, OrderPaymentMilestoneDto>(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Sales.Edit);
        await _repository.DeleteAsync(id);
    }
}
