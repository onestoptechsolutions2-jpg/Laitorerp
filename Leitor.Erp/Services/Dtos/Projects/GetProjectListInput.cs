using System;
using Leitor.Erp.Entities.Projects;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Projects;

public class GetProjectListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public ProjectStatus? Status { get; set; }
    public string? Filter { get; set; }
}
