using System;
using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class ProblemDto : FullAuditedEntityDto<Guid>
{
    public string ProblemNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProblemStatus Status { get; set; }
    public string? RootCause { get; set; }
    public string? Workaround { get; set; }
    public DateTime IdentifiedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    // Resolved by ProblemAppService from the Ticket repository - not a stored column.
    public int LinkedTicketCount { get; set; }
}
