using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Support;

namespace Leitor.Erp.Services.Dtos.Support;

public class CreateUpdateWarrantyClaimDto
{
    [Required]
    public Guid CustomerId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? JobId { get; set; }

    public Guid? TicketId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    public WarrantyClaimStatus Status { get; set; } = WarrantyClaimStatus.Open;

    [Required]
    public DateTime FiledDate { get; set; }
}
