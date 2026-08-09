using System;
using Leitor.Erp.Entities.Cybersecurity;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Cybersecurity;

public class GetSecurityAssessmentListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public SecurityAssessmentStatus? Status { get; set; }
    public string? Filter { get; set; }
}
