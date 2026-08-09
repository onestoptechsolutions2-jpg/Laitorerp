using System;
using Leitor.Erp.Entities.Governance;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Governance;

public class ChangeRequestDto : FullAuditedEntityDto<Guid>
{
    public Guid ConfigurationItemId { get; set; }
    public string ChangeNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChangeTier Tier { get; set; }
    public ChangeRequestStatus Status { get; set; }
    public Guid? TicketId { get; set; }
    public DateTime? PlannedDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CompletedDate { get; set; }
    public bool RolledBack { get; set; }
    public string? RollbackNotes { get; set; }
    public DateTime? PostImplementationReviewedDate { get; set; }

    // Resolved by ChangeRequestAppService from the ConfigurationItem/IdentityUser repositories -
    // not stored columns, same convention as VendorDto.WithholdingTaxRateName.
    public string? ConfigurationItemName { get; set; }
    public string? ApprovedByUserName { get; set; }
}
