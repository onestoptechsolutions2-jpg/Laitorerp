using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Projects;

public class GetProjectTaskListInput : PagedAndSortedResultRequestDto
{
    public Guid? ProjectId { get; set; }
}
