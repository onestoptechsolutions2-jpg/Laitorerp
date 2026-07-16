using System;
using Leitor.Erp.Entities.FieldService;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class FieldServiceJobDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ContractId { get; set; }
    public FieldServiceJobType Type { get; set; }
    public FieldServiceJobStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? SiteAddress { get; set; }
    public string? Description { get; set; }

    // Resolved by FieldServiceJobAppService from Customer/IdentityUser repositories - not stored
    // columns, Mapperly won't map them.
    public string? CustomerName { get; set; }
    public string? AssignedToUserName { get; set; }
}
