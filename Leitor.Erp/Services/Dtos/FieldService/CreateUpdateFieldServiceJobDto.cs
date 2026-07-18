using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.FieldService;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class CreateUpdateFieldServiceJobDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? ContractId { get; set; }

    public FieldServiceJobType Type { get; set; } = FieldServiceJobType.Installation;

    public FieldServiceJobStatus Status { get; set; } = FieldServiceJobStatus.Scheduled;

    [Required]
    public DateTime ScheduledDate { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public Guid? VendorId { get; set; }

    [StringLength(512)]
    public string? SiteAddress { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }
}
