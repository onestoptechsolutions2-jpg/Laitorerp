using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

public class CustomerTask : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    protected CustomerTask()
    {
    }

    public CustomerTask(Guid id, Guid customerId, string title)
        : base(id)
    {
        CustomerId = customerId;
        Title = title;
    }
}
