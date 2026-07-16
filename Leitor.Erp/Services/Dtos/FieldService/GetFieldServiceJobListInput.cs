using System;
using Leitor.Erp.Entities.FieldService;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class GetFieldServiceJobListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public FieldServiceJobStatus? Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Filter { get; set; }
}
