using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.FieldService;

public class FieldServiceJob : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ContractId { get; set; }
    public FieldServiceJobType Type { get; set; } = FieldServiceJobType.Installation;
    public FieldServiceJobStatus Status { get; set; } = FieldServiceJobStatus.Scheduled;
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? SiteAddress { get; set; }
    public string? Description { get; set; }

    protected FieldServiceJob()
    {
    }

    public FieldServiceJob(Guid id, Guid customerId)
        : base(id)
    {
        CustomerId = customerId;
    }
}
